namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para restablecer la contraseña con el token del enlace.
/// </summary>
public class RestablecerContrasenaRequest
{
    public string Token { get; set; } = null!;

    public string NuevaContrasena { get; set; } = null!;
}
