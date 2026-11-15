using Autiva.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Autiva.Infrastructure.Security;

/// <summary>
/// No envía correos reales: solo escribe el enlace en el log. Se usa en desarrollo y en
/// pruebas, cuando no hay configuración SMTP (Correo:Host).
/// </summary>
public class RegistroEnviadorCorreo : IEnviadorCorreo
{
    private readonly ILogger<RegistroEnviadorCorreo> _logger;

    public RegistroEnviadorCorreo(ILogger<RegistroEnviadorCorreo> logger)
    {
        _logger = logger;
    }

    public Task EnviarEnlaceRecuperacionAsync(string correoDestino, string enlace, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enlace de recuperación de contraseña para {Correo}: {Enlace}", correoDestino, enlace);
        return Task.CompletedTask;
    }
}
