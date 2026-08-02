using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NutritionApi.Api.Extensions;
using NutritionApi.Api.HealthChecks;
using NutritionApi.Api.Middleware;
using NutritionApi.Api.Startup;
using NutritionApi.Application;
using NutritionApi.Infrastructure;
using NutritionApi.Infrastructure.Persistence;
using NutritionApi.Infrastructure.Scheduling;
using Serilog;
using System.Reflection;

// Politique CORS appliquée aux appels du front — définie plus bas à partir de la configuration.
const string FrontCorsPolicy = "front";

// Points de terminaison de santé. Le préfixe sert aussi à les exclure de la redirection HTTPS.
const string HealthPath = "/health";
const string ReadyPath = "/health/ready";

// Marque les sondes que /health/ready exécute. Une sonde sans ce marqueur existe toujours, mais
// n'entre dans aucun verdict — c'est le tri entre vivacité et aptitude.
const string ReadyTag = "ready";

var builder = WebApplication.CreateBuilder(args);

// ── Journalisation ─────────────────────────────────────────────────────────────
// Serilog remplace les fournisseurs de sortie d'ASP.NET Core (NTR-137). Le code applicatif ne
// change pas : il continue d'écrire par ILogger, qui reste l'abstraction. Seule la mise en forme
// et la destination changent — JSON en production, texte lisible en développement.
//
// Tout se règle dans la section Serilog de la configuration, y compris les niveaux par catégorie :
// la section Logging n'est plus lue une fois ces fournisseurs remplacés.
//
// Aucune destination de collecte n'est branchée : le backend n'est pas arrêté. Le jour venu, un
// sink s'ajoute ici sans qu'une ligne de code applicatif bouge.
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

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

// ── Observabilité ──────────────────────────────────────────────────────────────
// Socle OpenTelemetry (NTR-140) : s'abonne aux mesures qu'ASP.NET Core, HttpClient, Npgsql et
// Redis émettent déjà. Après AddInfrastructure, dont l'instrumentation Redis attend le multiplexeur.
builder.Services.AddObservability(builder.Configuration, builder.Environment);

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
// Précharge les clés du realm pour que la première requête authentifiée n'attende pas. Un échec
// n'interrompt plus le démarrage depuis le 2026-08-02 (NTR-173) : c'est la sonde d'aptitude qui
// tient l'instance hors du service tant qu'elle n'a pas de clés.
builder.Services.AddHostedService<KeycloakAvailabilityService>();

// ── Sondes de santé ────────────────────────────────────────────────────────────
// Deux questions distinctes, deux points de terminaison (NTR-88) :
//   /health       suis-je vivant ?          — ne consulte rien
//   /health/ready suis-je en état de servir ? — consulte les dépendances marquées ReadyTag
//
// /health ne doit consulter aucune dépendance : c'est ce qui permet à un pod d'attendre ses clés
// sans être tué par la sonde de vivacité, et donc de rester en attente au lieu d'entrer dans une
// boucle de redémarrage.
builder.Services.AddHealthChecks()
    // Bloquante : sans base, l'application ne sait rien répondre.
    .AddDbContextCheck<AppDbContext>("postgresql", tags: [ReadyTag])
    // Bloquante, mais sur « ai-je des clés ? » et non « Keycloak répond-il ? » — voir la sonde.
    .AddCheck<SigningKeysHealthCheck>(SigningKeysHealthCheck.Name, tags: [ReadyTag])
    // Non bloquante : renvoie Degraded, publié dans la réponse, sans faire échouer l'aptitude.
    .AddCheck<RedisHealthCheck>(RedisHealthCheck.Name, HealthStatus.Degraded, tags: [ReadyTag]);

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

// Les sondes de santé sont interrogées en clair par le kubelet, sur le port du conteneur. Une
// redirection 307 vers HTTPS serait comptée comme un échec par l'orchestrateur — et un pod sain
// serait redémarré. D'où l'exclusion ; tout le reste du trafic redirige normalement.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments(HealthPath),
    branche => branche.UseHttpsRedirection());

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

// ── Sondes de santé ────────────────────────────────────────────────────────────
// Anonymes : le kubelet ne présente aucun jeton. UserResolutionMiddleware les laisse passer, il
// n'agit que sur les requêtes déjà authentifiées.
//
// Suis-je vivant ? Aucune sonde n'est exécutée : la seule chose vérifiée est que le processus
// répond. Une base injoignable ne doit pas faire tuer le conteneur — c'est ce qui permet à une
// instance d'attendre ses clés de signature au lieu d'entrer en boucle de redémarrage.
app.MapHealthChecks(HealthPath, new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthReportWriter.WriteAsync
}).AllowAnonymous();

// Suis-je en état de servir ? PostgreSQL et les clés de signature sont bloquants ; Redis est
// publié dégradé sans faire échouer l'aptitude — un statut Degraded répond 200.
app.MapHealthChecks(ReadyPath, new HealthCheckOptions
{
    Predicate = sonde => sonde.Tags.Contains(ReadyTag),
    ResponseWriter = HealthReportWriter.WriteAsync
}).AllowAnonymous();

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
