namespace Autiva.Api.Dtos;

/// <summary>
/// Datos enviados para iniciar sesión.
/// </summary>
public class LoginRequest
{
    public string Correo { get; set; } = null!;

    public string Contrasena { get; set; } = null!;
}
