namespace Autiva.Application.Interfaces;

/// <summary>
/// Envío de correos de la aplicación, sin acoplar el servicio de negocio al proveedor SMTP.
/// </summary>
public interface IEnviadorCorreo
{
    Task EnviarEnlaceRecuperacionAsync(string correoDestino, string enlace, CancellationToken cancellationToken = default);
}
