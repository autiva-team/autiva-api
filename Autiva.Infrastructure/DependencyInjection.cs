using Autiva.Application.Interfaces;
using Autiva.Infrastructure.Persistence;
using Autiva.Infrastructure.Repositories;
using Autiva.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Autiva.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AutivaDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IVehiculoRepository, VehiculoRepository>();
        services.AddScoped<ICargaRepository, CargaRepository>();
        services.AddScoped<IServicioRepository, ServicioRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IRecordatorioRepository, RecordatorioRepository>();
        services.AddScoped<IGastoRepository, GastoRepository>();
        services.AddScoped<ISincronizacionRepository, SincronizacionRepository>();
        services.AddScoped<IPushRepository, PushRepository>();
        services.AddScoped<IPushSender, WebPushSender>();
        services.AddScoped<ITokenRecuperacionRepository, TokenRecuperacionRepository>();
        services.AddScoped<IPreferenciasRepository, PreferenciasRepository>();

        // Sin configuración SMTP (Correo:Host), se usa el que solo registra el enlace en el
        // log: cubre desarrollo y pruebas sin depender de un servidor de correo real.
        if (string.IsNullOrWhiteSpace(configuration["Correo:Host"]))
            services.AddScoped<IEnviadorCorreo, RegistroEnviadorCorreo>();
        else
            services.AddScoped<IEnviadorCorreo, SmtpEnviadorCorreo>();

        return services;
    }
}
