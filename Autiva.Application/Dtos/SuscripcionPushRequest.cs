namespace Autiva.Application.Dtos;

/// <summary>
/// Datos para registrar o renovar una suscripción Web Push del navegador/dispositivo.
/// </summary>
public class SuscripcionPushRequest
{
    public string Endpoint { get; set; } = null!;
    public string ClaveP256dh { get; set; } = null!;
    public string ClaveAuth { get; set; } = null!;
    public string? Dispositivo { get; set; }
}
