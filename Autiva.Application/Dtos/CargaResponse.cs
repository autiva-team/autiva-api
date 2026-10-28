namespace Autiva.Application.Dtos;

/// <summary>
/// Datos de una carga de combustible devueltos al cliente.
/// </summary>
public class CargaResponse
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public string TipoCombustible { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public decimal Litros { get; set; }

    public decimal CostoTotal { get; set; }

    /// <summary>Calculado como CostoTotal / Litros; no se guarda en la base.</summary>
    public decimal PrecioPorLitro { get; set; }

    public int Odometro { get; set; }

    /// <summary>Odometro - odómetro de la carga anterior; null si es la primera carga del vehículo.</summary>
    public int? KmRecorridos { get; set; }

    public bool TanqueLleno { get; set; }

    /// <summary>Rendimiento km/L; null si no aplica (ver reglas de cálculo).</summary>
    public decimal? RendimientoKml { get; set; }

    public string? Gasolinera { get; set; }

    public string? Notas { get; set; }
}
