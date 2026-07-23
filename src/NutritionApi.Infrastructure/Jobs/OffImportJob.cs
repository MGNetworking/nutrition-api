namespace NutritionApi.Infrastructure.Jobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;

/// <summary>
/// Implémentation de <see cref="IOffImportJob"/> : lit le dump Open Food Facts au fil de l'eau
/// et alimente le catalogue <c>FoodItem</c> par lots (insertion ou mise à jour selon l'<c>OffId</c>).
/// </summary>
public sealed class OffImportJob : IOffImportJob
{
    private readonly IOffDumpReader _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OffImportJob> _logger;
    private readonly int _batchSize;

    /// <summary>Construit le job d'import.</summary>
    /// <param name="reader">Lecteur du dump Open Food Facts.</param>
    /// <param name="scopeFactory">Fabrique de scopes DI — un DbContext neuf par lot.</param>
    /// <param name="configuration">Configuration — <c>OpenFoodFacts:BatchSize</c> (défaut 1000).</param>
    /// <param name="logger">Journalisation du bilan d'import.</param>
    public OffImportJob(
        IOffDumpReader reader,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<OffImportJob> logger)
    {
        _reader = reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _batchSize = configuration.GetValue("OpenFoodFacts:BatchSize", 1000);
    }

    /// <summary>
    /// Parcourt le dump ligne par ligne, ignore les produits inexploitables, et persiste les
    /// produits valides par lots. Journalise le nombre de produits importés et ignorés.
    /// </summary>
    public async Task RunAsync()
    {
        var imported = 0;
        var skipped = 0;
        var batch = new List<OffProduct>(_batchSize);

        await foreach (var line in _reader.ReadLinesAsync())
        {
            var product = OffProductMapper.TryMap(line);
            if (product is null)
            {
                skipped++;
                continue;
            }

            batch.Add(product);
            if (batch.Count >= _batchSize)
            {
                imported += await PersistBatchAsync(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
            imported += await PersistBatchAsync(batch);

        _logger.LogInformation(
            "Import Open Food Facts terminé : {Imported} produits importés, {Skipped} ignorés.",
            imported, skipped);
    }

    private async Task<int> PersistBatchAsync(List<OffProduct> batch)
    {
        // Doublons éventuels dans le dump : une seule occurrence par code-barres (la dernière).
        var products = batch
            .GroupBy(p => p.OffId)
            .Select(g => g.Last())
            .ToList();

        // Scope dédié au lot : DbContext neuf → le ChangeTracker ne s'accumule pas d'un lot à
        // l'autre. La mémoire reste stable, que le dump fasse 100 000 ou 3 millions de lignes.
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFoodItemRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existing = (await repository.GetByOffIdsAsync(products.Select(p => p.OffId).ToList()))
            .ToDictionary(f => f.OffId);

        var toAdd = new List<FoodItem>();
        foreach (var product in products)
        {
            if (existing.TryGetValue(product.OffId, out var current))
            {
                current.UpdateFromImport(
                    product.Name, product.CaloriesPer100g,
                    product.ProteinsPer100g, product.CarbsPer100g,
                    product.FatsPer100g, product.AllergensTags);
            }
            else
            {
                toAdd.Add(new FoodItem(
                    product.OffId, product.Name, product.CaloriesPer100g,
                    product.ProteinsPer100g, product.CarbsPer100g,
                    product.FatsPer100g, product.AllergensTags));
            }
        }

        if (toAdd.Count > 0)
            await repository.AddRangeAsync(toAdd);

        await unitOfWork.SaveChangesAsync();
        return products.Count;
    }
}
