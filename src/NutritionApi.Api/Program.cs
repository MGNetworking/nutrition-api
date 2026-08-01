using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NutritionApi.Api.Extensions;
using NutritionApi.Api.Middleware;
using NutritionApi.Api.Startup;
using NutritionApi.Application;
using NutritionApi.Infrastructure;
using NutritionApi.Infrastructure.Scheduling;
using System.Reflection;

// Politique CORS appliquée aux appels du front — définie plus bas à partir de la configuration.
const string FrontCorsPolicy = "front";

var builder = WebApplication.CreateBuilder(args);

// ── Middlewares ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<RequestLoggingMiddleware>();
builder.Services.AddScoped<ExceptionMiddleware>();
builder.Services.AddScoped<UserResolutionMiddleware>();

// ── CORS ───────────────────────────────────────────────────────────────────────
// Seules les origines déclarées dans Cors:AllowedOrigins sont acceptées. Section absente
// ou vide = aucune origine autorisée : un oubli de configuration bloque, il n'ouvre pas.
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontCorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ── Application layer ──────────────────────────────────────────────────────────
builder.Services.AddApplication();

// ── Infrastructure layer ───────────────────────────────────────────────────────
// DbContext EF Core (PostgreSQL, snake_case), IUnitOfWork et repositories
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// ── Authentification ───────────────────────────────────────────────────────────
// KeycloakClaimsTransformation convertit realm_access.roles en ClaimTypes.Role standards
builder.Services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformation>();

// Valide le JWT Bearer via la clé publique Keycloak (Authority = issuer du token)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience  = builder.Configuration["Keycloak:Audience"];

        // Autorise un Authority en HTTP hors production (Keycloak local).
        // Défaut à true si la clé est absente : HTTPS exigé par défaut.
        options.RequireHttpsMetadata = builder.Configuration.GetValue("Keycloak:RequireHttpsMetadata", true);

        // Conserve les noms de claims d'origine du JWT. Sans cela, "sub" est remappé vers
        // ClaimTypes.NameIdentifier et FindFirstValue("sub") — utilisé par
        // UserResolutionMiddleware et les controllers — retourne null.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,  // Vérifie que le token provient bien de Keycloak
            ValidateAudience         = true,  // Vérifie que le token est destiné à cette API
            ValidateLifetime         = true,  // Vérifie que le token n'est pas expiré
            ValidateIssuerSigningKey = true   // Vérifie la signature avec la clé publique Keycloak
        };
    });

// ── Paramètres d'administration Keycloak ───────────────────────────────────────
// Vides dans appsettings.json par conception, renseignés par l'environnement. Leur absence ne se
// verrait qu'à la purge RGPD de la nuit suivante, dont l'échec est silencieux. Enregistré avant la
// vérification de disponibilité : lire quatre chaînes échoue tout de suite, elle attend le réseau.
builder.Services.AddHostedService<KeycloakAdminConfigurationValidator>();

// ── Disponibilité du serveur d'identité au démarrage ───────────────────────────
// La validation des jetons est locale, à partir des clés du realm mises en cache. Une instance
// démarrée sans avoir pu les récupérer accepterait le trafic et refuserait tous les jetons.
// Ce service force la récupération et interrompt le démarrage si elle n'aboutit pas.
builder.Services.AddHostedService<KeycloakAvailabilityService>();

// ── Autorisation ───────────────────────────────────────────────────────────────
// AdminOnly : réservé aux endpoints /api/v1/admin — rôle "admin" requis dans Keycloak
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nutrition API", Version = "v1" });

    // Active la prise en compte des attributs [SwaggerOperation] sur les actions
    options.EnableAnnotations();

    // Permet de saisir le token JWT directement dans l'UI Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        Description  = "Token JWT Keycloak — format : Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Charge les commentaires XML des controllers pour enrichir la doc Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    // Commentaires XML du projet Application (DTOs Request/Response — descriptions des schémas)
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "NutritionApi.Application.xml"));
});

// ── Build ──────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Pipeline HTTP ──────────────────────────────────────────────────────────────
// En tête de pipeline : c'est la seule position d'où la durée mesurée est celle de la
// requête entière, et d'où le statut lu est celui réellement renvoyé — y compris le 500
// écrit par ExceptionMiddleware ou la redirection émise par UseHttpsRedirection.
app.UseMiddleware<RequestLoggingMiddleware>(); // Trace méthode, route, statut et durée

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Nutrition API v1"));
}

app.UseHttpsRedirection();
app.UseCors(FrontCorsPolicy);                 // Restreint aux origines front configurées

app.UseMiddleware<ExceptionMiddleware>();      // Intercepte toutes les exceptions non gérées
app.UseAuthentication();                      // Valide le JWT Bearer
app.UseAuthorization();                       // Applique les policies et rôles

// Dashboard Hangfire — restreint au rôle admin par HangfireAdminAuthorizationFilter.
// Placé après UseAuthorization : le filtre lit HttpContext.User, peuplé par UseAuthentication.
//
// La dispense de profil est indispensable et ne va pas de soi. Cette ligne étant écrite avant
// UserResolutionMiddleware, on attendrait du dashboard qu'il réponde sans l'atteindre. C'est faux :
// l'endpoint est exécuté en fin de pipeline, après tous les middlewares déclarés. Sans dispense,
// un administrateur se voyait refuser le dashboard par un 401 tant qu'il n'avait pas de profil
// nutritionnel en base — or l'administration et l'espace client sont disjoints.
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()]
}).WithMetadata(new AllowWithoutProfileAttribute());

app.UseMiddleware<UserResolutionMiddleware>(); // Résout keycloakId → User.Id interne
app.MapControllers();

// Les jobs récurrents sont déclarés par RecurringJobRegistrationService (couche Infrastructure),
// enregistré par AddInfrastructure. Rien à faire ici.

app.Run();

/// <summary>
/// Rend la classe générée par les instructions de haut niveau accessible aux tests : sans cette
/// déclaration, <c>Program</c> reste <c>internal</c> et <c>WebApplicationFactory&lt;Program&gt;</c>
/// ne compile pas.
/// </summary>
public partial class Program;
