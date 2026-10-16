namespace NutritionApi.Infrastructure.Tests.Observability;

using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NutritionApi.Infrastructure.Observability;
using System.Data.Common;

/// <summary>
/// Vérifie la détection des requêtes lentes (NTR-141) : le seuil retenu, et la journalisation
/// dédiée qui en découle.
/// </summary>
/// <remarks>
/// Seule la décision est éprouvée ici, pas son branchement sur EF Core : elle est extraite dans une
/// méthode publique précisément pour être vérifiable sans base réelle — même approche que
/// <c>DatabaseExceptionInterceptor.Translate</c>. Le branchement relève du niveau 3.
/// </remarks>
[Trait("Level", "1")]
public class SlowQueryInterceptorTest
{
    private const string Requete = "select * from food_items where name like '%poulet%'";

    private static DbCommand Commande(string sql) => new NpgsqlCommand(sql);

    /// <summary>Un journaliseur strict n'est pas nécessaire : seule la présence des entrées compte.</summary>
    private static Mock<ILogger<SlowQueryInterceptor>> Journaliseur() => new();

    private static SlowQueryInterceptor Sujet(Mock<ILogger<SlowQueryInterceptor>> logger, int seuilMs = 500)
        => new(logger.Object, seuilMs);

    /// <summary>Vérifie qu'une entrée de ce niveau a été écrite, et son contenu.</summary>
    private static void VerifierEntree(Mock<ILogger<SlowQueryInterceptor>> logger, LogLevel niveau, Func<string, bool> contenu, Times fois)
        => logger.Verify(l => l.Log(
            niveau,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((message, _) => contenu(message.ToString()!)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), fois);

    // ---------------------------------------------------------------------
    // Chemin nominal — le seuil est franchi
    // ---------------------------------------------------------------------

    [Fact]
    public void Signaler_RequeteAuDelaDuSeuil_JournaliseUnAvertissement()
    {
        var logger = Journaliseur();

        Sujet(logger).Signaler(Commande(Requete), TimeSpan.FromMilliseconds(1250));

        VerifierEntree(logger, LogLevel.Warning, _ => true, Times.Once());
    }

    [Fact]
    public void Signaler_RequeteAuDelaDuSeuil_IndiqueLaDureeLeSeuilEtLeSql()
    {
        var logger = Journaliseur();

        Sujet(logger).Signaler(Commande(Requete), TimeSpan.FromMilliseconds(1250));

        // Sans la requête, l'avertissement ne mène à rien ; sans la durée ni le seuil, on ignore
        // de combien la requête dépasse — les trois composent la journalisation dédiée.
        VerifierEntree(
            logger,
            LogLevel.Warning,
            message => message.Contains("1250") && message.Contains("500") && message.Contains(Requete),
            Times.Once());
    }

    // ---------------------------------------------------------------------
    // Cas limites — le seuil lui-même
    // ---------------------------------------------------------------------

    [Fact]
    public void Signaler_RequeteExactementAuSeuil_EstSignalee()
    {
        var logger = Journaliseur();

        Sujet(logger, seuilMs: 500).Signaler(Commande(Requete), TimeSpan.FromMilliseconds(500));

        // Le seuil est inclusif : une requête « à 500 ms » est lente pour un seuil de 500 ms.
        VerifierEntree(logger, LogLevel.Warning, _ => true, Times.Once());
    }

    [Fact]
    public void Signaler_RequeteJusteSousLeSeuil_NeJournaliseRien()
    {
        var logger = Journaliseur();

        Sujet(logger, seuilMs: 500).Signaler(Commande(Requete), TimeSpan.FromMilliseconds(499));

        // Une requête rapide ne doit rien produire : journaliser chaque commande noierait la
        // console et ferait perdre l'intérêt du seuil.
        logger.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never());
    }

    // ---------------------------------------------------------------------
    // Cas d'erreur — le seuil configuré
    // ---------------------------------------------------------------------

    [Fact]
    public void Constructeur_SeuilNul_EstRefuse()
    {
        var logger = Journaliseur();

        // Un seuil nul signalerait chacune des requêtes : la garde du seuil ne peut pas être
        // contournée par une configuration erronée.
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlowQueryInterceptor(logger.Object, seuilMillisecondes: 0));
    }

    [Fact]
    public void Constructeur_SeuilNegatif_EstRefuse()
    {
        var logger = Journaliseur();

        Assert.Throws<ArgumentOutOfRangeException>(() => new SlowQueryInterceptor(logger.Object, seuilMillisecondes: -1));
    }
}
