namespace NutritionApi.ExternalIntegration.Tests.Fixtures;

using System.Text.Json;

/// <summary>
/// Lit <c>keycloak/realm-export.json</c> — la source de vérité des identités du realm.
/// </summary>
/// <remarks>
/// Ces valeurs étaient auparavant recopiées en constantes dans les fixtures. Elles existaient donc
/// à trois endroits : l'export du realm, les tests, et <c>seed-dev.sql</c>. Un renommage de compte
/// dans l'export laissait les tests pointer vers un identifiant obsolète, sans que rien ne le
/// signale avant l'échec.
/// <para>
/// Le fichier est copié dans le répertoire de sortie à la compilation : les tests lisent une copie
/// produite depuis la source, jamais une valeur ressaisie.
/// </para>
/// <para>
/// Rien n'est secret ici. Le realm de développement est jetable et réimporté à chaque recréation du
/// conteneur ; son export est versionné dans le dépôt.
/// </para>
/// </remarks>
public static class RealmExport
{
    private static readonly Lazy<JsonElement> Document = new(Charger);

    /// <summary>Nom du realm — la valeur du champ <c>realm</c>.</summary>
    public static string RealmName => Document.Value.GetProperty("realm").GetString()!;

    /// <summary>
    /// Identifiant Keycloak d'un compte, celui qui alimente la colonne <c>keycloak_id</c>.
    /// </summary>
    /// <param name="username">Nom du compte, tel que déclaré dans l'export.</param>
    /// <returns>Le <c>sub</c> que porteront les jetons de ce compte.</returns>
    /// <exception cref="InvalidOperationException">Le compte n'existe pas dans l'export.</exception>
    public static string SubjectOf(string username) =>
        Utilisateur(username).GetProperty("id").GetString()
        ?? throw new InvalidOperationException($"Le compte « {username} » n'a pas d'identifiant dans l'export du realm.");

    /// <summary>Mot de passe d'un compte, lu depuis ses <c>credentials</c>.</summary>
    /// <param name="username">Nom du compte.</param>
    /// <returns>Le mot de passe en clair — realm de développement, valeur non sensible.</returns>
    /// <exception cref="InvalidOperationException">Le compte n'a pas d'identifiant de type mot de passe.</exception>
    public static string PasswordOf(string username)
    {
        foreach (var identifiant in Utilisateur(username).GetProperty("credentials").EnumerateArray())
        {
            if (identifiant.GetProperty("type").GetString() == "password")
                return identifiant.GetProperty("value").GetString()!;
        }

        throw new InvalidOperationException($"Le compte « {username} » n'a pas de mot de passe dans l'export du realm.");
    }

    /// <summary>Secret d'un client confidentiel.</summary>
    /// <param name="clientId">Identifiant du client.</param>
    /// <returns>Le secret déclaré dans l'export.</returns>
    /// <exception cref="InvalidOperationException">Le client n'existe pas, ou n'a pas de secret.</exception>
    public static string SecretOf(string clientId)
    {
        foreach (var client in Document.Value.GetProperty("clients").EnumerateArray())
        {
            if (client.GetProperty("clientId").GetString() != clientId)
                continue;

            return client.TryGetProperty("secret", out var secret)
                ? secret.GetString()!
                : throw new InvalidOperationException($"Le client « {clientId} » n'a pas de secret : il est public.");
        }

        throw new InvalidOperationException($"Le client « {clientId} » n'existe pas dans l'export du realm.");
    }

    /// <summary>Vérifie qu'un client est bien déclaré — garde-fou contre une fixture désynchronisée.</summary>
    /// <param name="clientId">Identifiant du client.</param>
    /// <returns>L'identifiant lui-même, pour un usage en cascade.</returns>
    /// <exception cref="InvalidOperationException">Le client n'existe pas dans l'export.</exception>
    public static string RequireClient(string clientId)
    {
        foreach (var client in Document.Value.GetProperty("clients").EnumerateArray())
        {
            if (client.GetProperty("clientId").GetString() == clientId)
                return clientId;
        }

        throw new InvalidOperationException(
            $"Le client « {clientId} » n'existe pas dans keycloak/realm-export.json. "
            + "La fixture et le realm ont divergé.");
    }

    /// <summary>Retrouve un compte par son nom, en ignorant les comptes de service.</summary>
    /// <param name="username">Nom du compte.</param>
    private static JsonElement Utilisateur(string username)
    {
        foreach (var utilisateur in Document.Value.GetProperty("users").EnumerateArray())
        {
            if (utilisateur.GetProperty("username").GetString() == username)
                return utilisateur;
        }

        throw new InvalidOperationException(
            $"Le compte « {username} » n'existe pas dans keycloak/realm-export.json.");
    }

    /// <summary>Charge l'export copié à côté de l'assembly de tests.</summary>
    /// <exception cref="InvalidOperationException">Le fichier est absent — la copie du csproj a échoué.</exception>
    private static JsonElement Charger()
    {
        var chemin = Path.Combine(AppContext.BaseDirectory, "realm-export.json");

        if (!File.Exists(chemin))
        {
            throw new InvalidOperationException(
                $"L'export du realm est introuvable ({chemin}). "
                + "Il doit être copié dans la sortie de compilation — voir le csproj du projet de tests.");
        }

        return JsonDocument.Parse(File.ReadAllText(chemin)).RootElement.Clone();
    }
}
