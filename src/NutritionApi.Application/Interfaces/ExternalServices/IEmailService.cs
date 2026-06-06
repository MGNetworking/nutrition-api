namespace NutritionApi.Application.Interfaces.ExternalServices;

public interface IEmailService
{
    Task SendReactivationEmailAsync(string toEmail, string reactivationLink, TimeSpan validity);
}
