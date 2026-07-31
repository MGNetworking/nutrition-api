namespace NutritionApi.Api.Tests.Level1;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Controllers;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;

/// <summary>
/// Verrouille le contrat OpenAPI au niveau des attributs, sans démarrer l'API : chaque action
/// exposée doit décrire ce qu'elle fait et tous les statuts qu'elle peut renvoyer. La validation
/// du <c>swagger.json</c> produit en conditions réelles reste un test de niveau 3 (NTR-28).
/// </summary>
[Trait("Level", "1")]
public class OpenApiContractTest
{
    /// <summary>Toutes les actions publiques exposées par les controllers de l'API.</summary>
    public static TheoryData<string, string> Actions
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (var controller in ControllerTypes())
                foreach (var action in ActionMethods(controller))
                    data.Add(controller.Name, action.Name);

            return data;
        }
    }

    private static IEnumerable<Type> ControllerTypes()
        => typeof(AdminController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    private static IEnumerable<MethodInfo> ActionMethods(Type controller)
        => controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.GetCustomAttribute<NonActionAttribute>() is null);

    private static MethodInfo Resolve(string controllerName, string actionName)
        => ActionMethods(ControllerTypes().Single(t => t.Name == controllerName))
            .Single(m => m.Name == actionName);

    [Fact]
    public void Api_ExposesAtLeastOneController()
        => Assert.NotEmpty(ControllerTypes());

    [Theory]
    [MemberData(nameof(Actions))]
    public void Action_DeclaresSummaryAndDescription(string controllerName, string actionName)
    {
        var operation = Resolve(controllerName, actionName).GetCustomAttribute<SwaggerOperationAttribute>();

        Assert.NotNull(operation);
        Assert.False(string.IsNullOrWhiteSpace(operation.Summary), $"{controllerName}.{actionName} : Summary vide.");
        Assert.False(string.IsNullOrWhiteSpace(operation.Description), $"{controllerName}.{actionName} : Description vide.");
    }

    [Theory]
    [MemberData(nameof(Actions))]
    public void Action_DeclaresSuccessResponseType(string controllerName, string actionName)
    {
        var responses = Resolve(controllerName, actionName)
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToList();

        Assert.True(
            responses.Any(code => code is >= 200 and < 300),
            $"{controllerName}.{actionName} : aucun ProducesResponseType de succès.");
    }

    [Theory]
    [MemberData(nameof(Actions))]
    public void Action_DeclaresUnauthorizedWhenAuthenticationRequired(string controllerName, string actionName)
    {
        var method = Resolve(controllerName, actionName);

        if (!RequiresAuthentication(method))
            return;

        var responses = method
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode);

        Assert.True(
            responses.Contains(StatusCodes.Status401Unauthorized),
            $"{controllerName}.{actionName} : protégée par [Authorize] mais ne déclare pas le 401.");
    }

    /// <summary>Indique si l'action est protégée, directement ou par son controller.</summary>
    private static bool RequiresAuthentication(MethodInfo method)
    {
        if (method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            return false;

        return method.GetCustomAttribute<AuthorizeAttribute>() is not null
            || method.DeclaringType!.GetCustomAttribute<AuthorizeAttribute>() is not null;
    }
}
