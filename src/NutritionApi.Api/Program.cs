using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NutritionApi.Api.Extensions;
using NutritionApi.Api.Middleware;
using NutritionApi.Application;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Ajout des dépendance dans mon conteneur d'injection

builder.Services.AddScoped<ExceptionMiddleware>();

builder.Services.AddApplication();
// ASP.NET Core le projet utilise des controllers MVC
builder.Services.AddControllers(); 

// Utilisé automatiquement par UseAuthentication
builder.Services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformation>();

// Active les régles est vérifie l'authenticité du token en utilisant la clé publique de Keycloak
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Active la vérification de l'émetteur
            ValidateAudience = true,         // Active la vérification du destinataire
            ValidateLifetime = true,         // Active la vérification de l'expiration
            ValidateIssuerSigningKey = true  // Active la vérification de la signature
        };
    });

// C'est la règle d'autorisation de end point 
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Nutrition API",
        Version = "v1"
    });

    // Authentification JWT dans l'UI Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token JWT Keycloak — format : Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Commentaires XML pour la documentation des endpoints
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

});

builder.Services.AddScoped<UserResolutionMiddleware>();

// Construction de l'application
var app = builder.Build();


// Uniquement hors production
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Nutrition API v1"));
}


// pipeline de traitement des requêtes HTTP

app.UseHttpsRedirection();  // inclus dans le Web SDK
// TODO : configurer AddCors() dans builder.Services
//app.UseCors();             // Autorise les requêtes cross-origin

app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();   // Valide le JWT Bearer
app.UseAuthorization();    // Vérifie les rôles / policies

app.UseMiddleware<UserResolutionMiddleware>(); // Résout le keycloakId du token vers le User.Id interne
app.MapControllers();      // Scanner tous tes controllers et d'enregistrer leurs routes dans le routing engine

app.Run();
