using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MarketWorkplace.Api.Swagger;

/// <summary>
/// The global Bearer requirement would otherwise mark <c>[AllowAnonymous]</c> operations
/// (like login) as "token required". An empty <c>security</c> list serializes as
/// <c>"security": []</c>, which explicitly means "no authentication required".
/// </summary>
public class AllowAnonymousOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var isAnonymous =
            method.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any() ||
            method.DeclaringType?.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any() == true;

        if (isAnonymous)
        {
            operation.Security = [];
        }
    }
}
