using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NutritionApi.Api.Extensions;
using NutritionApi.Api.Middleware;
using NutritionApi.Application;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Infrastructure;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.Infrastructure.Jobs.RgpdPurge;
using NutritionApi.Infrastructure.Scheduling;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ── Middlewares ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ExceptionMiddleware>();
builder.Services.AddScoped<UserResolutionMiddleware>();

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
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Nutrition API v1"));
}

app.UseHttpsRedirection();
// TODO : configurer AddCors() dans builder.Services
// app.UseCors();

app.UseMiddleware<ExceptionMiddleware>();      // Intercepte toutes les exceptions non gérées
app.UseAuthentication();                      // Valide le JWT Bearer
app.UseAuthorization();                       // Applique les policies et rôles

// Dashboard Hangfire — restreint au rôle admin par HangfireAdminAuthorizationFilter.
// Placé après UseAuthorization (le filtre lit User) et avant UserResolutionMiddleware
// (le dashboard n'a pas besoin de la résolution keycloakId → User interne).
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()]
});

app.UseMiddleware<UserResolutionMiddleware>(); // Résout keycloakId → User.Id interne
app.MapControllers();

// Job d'import Open Food Facts — chaque nuit à 03h00 UTC (volet Remplissage de NTR-55).
// Enregistré ici pour que le storage Hangfire soit prêt ; il apparaît dès lors dans la supervision.
RecurringJob.AddOrUpdate<IOffImportJob>(
    IJobMonitoringService.ImportOffJobName,
    job => job.RunAsync(),
    Cron.Daily(3),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

// Job de purge RGPD — chaque nuit à 03h30 UTC (NTR-56), décalé de l'import pour ne pas
// concurrencer son écriture en base.
RecurringJob.AddOrUpdate<IRgpdPurgeJob>(
    IJobMonitoringService.RgpdPurgeJobName,
    job => job.RunAsync(),
    "30 3 * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

app.Run();
