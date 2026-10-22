namespace Autiva.Application.Dtos;

/// <summary>
/// Datos de la cuenta creada. Nunca incluye el hash de la contraseña.
/// </summary>
public class RegistroUsuarioResponse
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string TipoCuenta { get; set; } = null!;

    public DateTime CreadoEn { get; set; }
}
