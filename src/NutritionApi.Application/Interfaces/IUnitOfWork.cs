namespace NutritionApi.Application.Interfaces;

/// <summary>Contrat de persistance transactionnelle — valide toutes les modifications en attente.</summary>
public interface IUnitOfWork
{
    /// <summary>Persiste toutes les modifications en attente dans la base de données.</summary>
    Task SaveChangesAsync();
}
