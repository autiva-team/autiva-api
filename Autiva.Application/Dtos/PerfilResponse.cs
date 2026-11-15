namespace Autiva.Application.Dtos;

/// <summary>
/// Datos del perfil del usuario autenticado.
/// </summary>
public class PerfilResponse
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string TipoCuenta { get; set; } = null!;

    /// <summary>Vehículos no borrados del usuario.</summary>
    public int NumeroVehiculos { get; set; }

    public DateTime CreadoEn { get; set; }
}
