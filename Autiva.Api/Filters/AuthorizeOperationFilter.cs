using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Autiva.Api.Filters;

/// <summary>
/// Agrega el requisito de seguridad Bearer solo a los endpoints
/// protegidos con [Authorize], para que Swagger muestre el candado
/// únicamente donde corresponde.
/// </summary>
public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var tieneAuthorize =
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
            (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false);

        var tieneAllowAnonymous =
            context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

        if (tieneAuthorize && !tieneAllowAnonymous)
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                }
            };
        }
    }
}
