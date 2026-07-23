namespace NutritionApi.Infrastructure.Jobs;

using System.IO.Compression;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

/// <summary>Fournit les lignes du dump Open Food Facts, une par une.</summary>
public interface IOffDumpReader
{
    /// <summary>Télécharge le dump et retourne ses lignes JSON au fil de l'eau.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Un flux asynchrone de lignes JSON non vides.</returns>
    IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Lit le dump JSONL compressé (gzip) d'Open Food Facts en streaming — le fichier n'est
/// jamais chargé intégralement en mémoire.
/// </summary>
public sealed class OffDumpReader : IOffDumpReader
{
    private readonly HttpClient _httpClient;
    private readonly string _dumpUrl;

    /// <summary>Construit le lecteur à partir de l'URL configurée dans <c>OpenFoodFacts:DumpUrl</c>.</summary>
    /// <param name="httpClient">Client HTTP injecté.</param>
    /// <param name="configuration">Configuration de l'application.</param>
    /// <exception cref="InvalidOperationException">La configuration <c>OpenFoodFacts:DumpUrl</c> est absente.</exception>
    public OffDumpReader(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _dumpUrl = configuration["OpenFoodFacts:DumpUrl"]
            ?? throw new InvalidOperationException("La configuration 'OpenFoodFacts:DumpUrl' est absente.");
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ReadLinesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            _dumpUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                yield return line;
        }
    }
}
