namespace Autiva.Application.Dtos;

/// <summary>
/// Datos de un vehículo devueltos al cliente.
/// </summary>
public class VehiculoResponse
{
    public Guid Id { get; set; }

    public string Marca { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public short Anio { get; set; }

    public string Placas { get; set; } = null!;

    public string? Color { get; set; }

    public int KilometrajeInicial { get; set; }

    public int OdometroActual { get; set; }

    public string Estatus { get; set; } = null!;

    /// <summary>Indica si es el vehículo activo/principal del usuario.</summary>
    public bool EsPrincipal { get; set; }

    public string? FotoUrl { get; set; }

    public DateTime CreadoEn { get; set; }
}
