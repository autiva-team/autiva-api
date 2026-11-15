using Autiva.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Autiva.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IVehiculoService, VehiculoService>();
        services.AddScoped<ICargaService, CargaService>();
        services.AddScoped<IRendimientoService, RendimientoService>();
        services.AddScoped<IServicioService, ServicioService>();
        services.AddScoped<IRecordatorioService, RecordatorioService>();
        services.AddScoped<IGastoService, GastoService>();
        services.AddScoped<ISincronizacionService, SincronizacionService>();
        services.AddScoped<IPushService, PushService>();

        return services;
    }
}
