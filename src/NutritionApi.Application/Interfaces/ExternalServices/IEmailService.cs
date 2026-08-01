namespace NutritionApi.Application.Interfaces.ExternalServices;

/// <summary>Contrat d'envoi d'e-mails transactionnels.</summary>
public interface IEmailService
{
    /// <summary>Envoie un e-mail de réactivation de compte à l'utilisateur.</summary>
    /// <param name="toEmail">Adresse e-mail du destinataire.</param>
    /// <param name="reactivationLink">Lien de réactivation du compte.</param>
    /// <param name="validity">Durée de validité du lien.</param>
    Task SendReactivationEmailAsync(string toEmail, string reactivationLink, TimeSpan validity);
}
