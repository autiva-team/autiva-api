namespace Autiva.Api.Dtos;

/// <summary>
/// Datos devueltos al iniciar sesión correctamente.
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = null!;

    public DateTime ExpiraEn { get; set; }

    public Guid UsuarioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string TipoCuenta { get; set; } = null!;
}
