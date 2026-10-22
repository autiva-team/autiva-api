using Autiva.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Autiva.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
