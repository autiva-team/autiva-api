namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para solicitar el enlace de recuperación de contraseña.
/// </summary>
public class RecuperarContrasenaRequest
{
    public string Correo { get; set; } = null!;
}
