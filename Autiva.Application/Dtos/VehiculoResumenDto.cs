namespace Autiva.Application.Dtos;

/// <summary>
/// Datos resumidos de un vehículo para el listado del usuario autenticado.
/// </summary>
public class VehiculoResumenDto
{
    public Guid Id { get; set; }

    public string Marca { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public string Placas { get; set; } = null!;

    public int OdometroActual { get; set; }

    public string Estatus { get; set; } = null!;

    /// <summary>Indica si es el vehículo activo/principal del usuario.</summary>
    public bool EsPrincipal { get; set; }
}
