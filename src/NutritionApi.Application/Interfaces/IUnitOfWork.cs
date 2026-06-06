namespace NutritionApi.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
