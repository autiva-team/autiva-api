using System.Net;
using System.Net.Mail;
using Autiva.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Autiva.Infrastructure.Security;

/// <summary>
/// Envía correos reales por SMTP. La configuración viene de Correo:Host, Correo:Puerto,
/// Correo:Usuario, Correo:Contrasena y Correo:Remitente (user-secrets en local, variables
/// de entorno en Azure; nunca en appsettings.json ni en el código).
/// </summary>
public class SmtpEnviadorCorreo : IEnviadorCorreo
{
    private readonly IConfiguration _configuration;

    public SmtpEnviadorCorreo(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task EnviarEnlaceRecuperacionAsync(string correoDestino, string enlace, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Correo:Host"];
        var puerto = _configuration.GetValue<int?>("Correo:Puerto") ?? 587;
        var usuario = _configuration["Correo:Usuario"];
        var contrasena = _configuration["Correo:Contrasena"];
        var remitente = _configuration["Correo:Remitente"] ?? usuario ?? string.Empty;

        using var cliente = new SmtpClient(host, puerto)
        {
            Credentials = new NetworkCredential(usuario, contrasena),
            EnableSsl = true
        };

        using var mensaje = new MailMessage(remitente, correoDestino)
        {
            Subject = "Recupera tu contraseña de Autiva",
            Body = $"Recibimos una solicitud para restablecer tu contraseña.\n\n" +
                   $"Da clic en este enlace para continuar: {enlace}\n\n" +
                   $"El enlace expira en 30 minutos. Si tú no lo solicitaste, ignora este correo.",
            IsBodyHtml = false
        };

        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}
