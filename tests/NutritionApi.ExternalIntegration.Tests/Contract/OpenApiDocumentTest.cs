namespace NutritionApi.ExternalIntegration.Tests.Contract;

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// Le document OpenAPI réellement produit par Swashbuckle, lu sur l'API démarrée.
/// </summary>
/// <remarks>
/// <c>OpenApiContractTest</c>, au niveau 1, parcourt les types par réflexion : il valide les
/// <b>attributs</b> posés sur les actions, jamais le document généré. Un défaut de sérialisation lui
/// échapperait entièrement — le <c>swagger.json</c> pourrait être invalide ou incomplet sans
/// qu'aucun test ne le voie. Seule une API démarrée produit le document que consommeront Postman,
/// NSwag ou Kiota.
/// <para>
/// Le document est servi sans authentification : <c>UseSwagger</c> est déclaré avant
/// <c>UseAuthentication</c> dans le pipeline, et l'environnement du niveau 3 n'est pas la
/// production.
/// </para>
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class OpenApiDocumentTest(IntegrationFactory factory)
{
    /// <summary>Chemin du document, tel que Swagger UI le référence.</summary>
    private const string DocumentPath = "/swagger/v1/swagger.json";

    /// <summary>
    /// le document est servi, et annoncé comme du JSON.
    /// </summary>
    [Fact]
    public async Task GetSwaggerJson_ShouldReturn200_WhenApiIsRunning()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(DocumentPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// le document est lu sans erreur par un parseur OpenAPI.
    /// </summary>
    /// <remarks>
    /// C'est le cas qui répond à « importable ». Un document que <c>OpenApiStringReader</c> accepte
    /// sans diagnostic est celui que les générateurs de clients savent consommer ; vérifier
    /// seulement que le JSON est bien formé ne prouverait rien de sa validité OpenAPI.
    /// </remarks>
    [Fact]
    public async Task GetSwaggerJson_ShouldParseWithoutError_WhenReadByAnOpenApiReader()
    {
        var (document, diagnostic) = await LireLeDocumentAsync();

        Assert.Empty(diagnostic.Errors.Select(erreur => $"{erreur.Pointer} : {erreur.Message}"));
        Assert.NotNull(document.Info);
        Assert.Equal("Nutrition API", document.Info.Title);
    }

    /// <summary>
    /// chaque action exposée par l'API figure dans le document.
    /// </summary>
    /// <remarks>
    /// La référence n'est pas une liste écrite à la main mais les <c>ApiDescription</c> que le
    /// routage MVC produit réellement. Le test suit donc l'API sans maintenance : une action ajoutée
    /// est attendue dans le document dès le run suivant, et une action que Swashbuckle laisserait
    /// tomber est signalée nommément.
    /// </remarks>
    [Fact]
    public async Task GetSwaggerJson_ShouldDeclareEveryApiDescription_WhenDocumentIsGenerated()
    {
        var (document, _) = await LireLeDocumentAsync();

        var declares = document.Paths.Keys.Select(Normaliser).ToHashSet();

        var attendus = factory.Services
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(groupe => groupe.Items)
            .Select(description => Normaliser($"/{description.RelativePath}"))
            .Distinct()
            .ToList();

        Assert.NotEmpty(attendus);

        var manquants = attendus.Where(chemin => !declares.Contains(chemin)).ToList();

        Assert.True(
            manquants.Count == 0,
            $"Absents du document : {string.Join(", ", manquants)}.\n"
            + $"Chemins déclarés : {string.Join(", ", declares.Order())}");
    }

    /// <summary>
    /// le schéma de sécurité Bearer est déclaré dans le document.
    /// </summary>
    /// <remarks>
    /// Sans lui, Swagger UI n'offrirait pas de champ où coller un jeton, et les clients générés
    /// n'auraient aucun moyen de s'authentifier. C'est déclaré dans <c>AddSwaggerGen</c> ; ce test
    /// vérifie que la déclaration arrive jusqu'au document.
    /// </remarks>
    [Fact]
    public async Task GetSwaggerJson_ShouldDeclareBearerSecurityScheme_WhenDocumentIsGenerated()
    {
        var (document, _) = await LireLeDocumentAsync();

        var schema = Assert.Contains("Bearer", document.Components.SecuritySchemes);

        Assert.Equal(SecuritySchemeType.Http, schema.Type);
        Assert.Equal("bearer", schema.Scheme);
    }

    /// <summary>
    /// le dashboard Hangfire n'apparaît pas dans le document.
    /// </summary>
    /// <remarks>
    /// <c>MapHangfireDashboard</c> monte un middleware, pas une action de controller : il ne produit
    /// aucune <c>ApiDescription</c> et reste donc invisible pour la génération, sans qu'aucun filtre
    /// n'ait à l'exclure. La documentation l'affirme depuis l'origine ; rien ne le vérifiait. Le jour
    /// où une route d'administration serait montée en action, ce test le signalerait.
    /// </remarks>
    [Fact]
    public async Task GetSwaggerJson_ShouldNotDeclareHangfireDashboard_WhenDocumentIsGenerated()
    {
        var (document, _) = await LireLeDocumentAsync();

        Assert.DoesNotContain(
            document.Paths.Keys,
            chemin => chemin.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Télécharge le document et le fait lire par le parseur OpenAPI.</summary>
    /// <returns>Le document lu et le diagnostic de lecture.</returns>
    private async Task<(OpenApiDocument Document, OpenApiDiagnostic Diagnostic)> LireLeDocumentAsync()
    {
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync(DocumentPath);
        var document = new OpenApiStringReader().Read(json, out var diagnostic);

        return (document, diagnostic);
    }

    /// <summary>
    /// Ramène un chemin de route à une forme comparable entre les deux sources.
    /// </summary>
    /// <param name="chemin">Chemin issu du document ou d'une <c>ApiDescription</c>.</param>
    /// <returns>Le chemin en minuscules, contraintes de route retirées.</returns>
    /// <remarks>
    /// Une <c>ApiDescription</c> peut porter la contrainte du paramètre — <c>{id:guid}</c> — là où le
    /// document n'expose que son nom. Comparer les deux formes brutes ferait échouer le test sur une
    /// différence de notation, sans rapport avec ce qu'il vérifie.
    /// </remarks>
    private static string Normaliser(string chemin)
        => Regex.Replace(chemin, @"\{(\w+)[^}]*\}", "{$1}").ToLowerInvariant();
}
