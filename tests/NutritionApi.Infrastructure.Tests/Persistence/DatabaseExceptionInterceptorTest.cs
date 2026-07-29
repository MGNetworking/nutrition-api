namespace NutritionApi.Infrastructure.Tests.Persistence;

using Npgsql;
using NutritionApi.Application.Exceptions;
using NutritionApi.Infrastructure.Persistence.Interceptors;
using System.Net.Sockets;

/// <summary>
/// Vérifie la discrimination des échecs PostgreSQL — NTR-135, lot C.
/// </summary>
/// <remarks>
/// Seule la traduction est testée ici, pas son branchement sur EF Core : elle est extraite dans une
/// méthode pure précisément pour être éprouvable sans base réelle. Le branchement relève du niveau 3.
/// </remarks>
public class DatabaseExceptionInterceptorTest
{
    [Fact]
    public void Translate_ViolationDUnicite_DonneUnConflit()
    {
        // 23505 = unique_violation dans la nomenclature PostgreSQL.
        var exception = CreatePostgresException("23505");

        var traduite = DatabaseExceptionInterceptor.Translate(exception);

        Assert.IsType<ConflictException>(traduite);
    }

    [Fact]
    public void Translate_ViolationDeCleEtrangere_NestPasTraduite()
    {
        // 23503 = foreign_key_violation : un défaut de cohérence applicative, pas un conflit
        // que le client puisse résoudre en changeant sa saisie.
        var exception = CreatePostgresException("23503");

        var traduite = DatabaseExceptionInterceptor.Translate(exception);

        Assert.Null(traduite);
    }

    [Fact]
    public void Translate_BaseInjoignable_DonneUneIndisponibilite()
    {
        var exception = new NpgsqlException("connexion refusée", new SocketException(10061));

        var traduite = DatabaseExceptionInterceptor.Translate(exception);

        var indisponible = Assert.IsType<ServiceUnavailableException>(traduite);
        Assert.Equal("PostgreSQL", indisponible.Dependency);
    }

    [Fact]
    public void Translate_ExceptionEnveloppeeParEfCore_EstQuandMemeReconnue()
    {
        // Sur SaveChanges, EF Core enveloppe l'erreur Npgsql : la discrimination doit regarder
        // l'exception interne, pas le type externe.
        var interne = CreatePostgresException("23505");
        var enveloppee = new Microsoft.EntityFrameworkCore.DbUpdateException("échec", interne);

        var traduite = DatabaseExceptionInterceptor.Translate(enveloppee);

        Assert.IsType<ConflictException>(traduite);
    }

    [Fact]
    public void Translate_ExceptionSansRapport_NestPasTraduite()
    {
        var traduite = DatabaseExceptionInterceptor.Translate(new InvalidOperationException("autre chose"));

        // Ne rien traduire laisse remonter l'exception d'origine, donc un 500 : c'est le
        // comportement voulu pour ce qui n'est pas identifié.
        Assert.Null(traduite);
    }

    /// <summary>Construit une <see cref="PostgresException"/> portant le code SQL voulu.</summary>
    private static PostgresException CreatePostgresException(string sqlState)
        => new(messageText: "erreur", severity: "ERROR", invariantSeverity: "ERROR", sqlState: sqlState);
}
