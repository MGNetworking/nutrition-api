using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NutritionApi.Api.Extensions;
using NutritionApi.Api.Middleware;
using NutritionApi.Application;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ── Middlewares ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ExceptionMiddleware>();
builder.Services.AddScoped<UserResolutionMiddleware>();

// ── Application layer ──────────────────────────────────────────────────────────
builder.Services.AddApplication();
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
app.UseMiddleware<UserResolutionMiddleware>(); // Résout keycloakId → User.Id interne
app.MapControllers();

app.Run();
