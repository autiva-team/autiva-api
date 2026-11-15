namespace Autiva.Application.Dtos;

/// <summary>
/// Avance hacia el próximo servicio de mantenimiento de un vehículo, según la meta
/// de kilometraje (meta_proximo_km) del último servicio que la tenga.
/// </summary>
public class AvanceServicioResponse
{
    /// <summary>Último servicio no borrado con meta; null si el vehículo no tiene ninguno.</summary>
    public UltimoServicioConMetaDto? UltimoServicio { get; set; }

    public int? MetaKm { get; set; }

    public int OdometroActual { get; set; }

    /// <summary>MetaKm - OdometroActual; puede ser negativo si ya se pasó.</summary>
    public int? KmRestantes { get; set; }

    /// <summary>Entre 0 y 100, redondeado a 1 decimal.</summary>
    public decimal? PorcentajeAvance { get; set; }

    public bool? Vencido { get; set; }

    /// <summary>Promedio de km recorridos por día desde el servicio hasta hoy (hora de México).</summary>
    public decimal? KmPorDia { get; set; }

    /// <summary>Días estimados para llegar a la meta; null si no hay km por día o ya está vencido.</summary>
    public int? DiasEstimados { get; set; }

    public DateOnly? FechaEstimada { get; set; }

    /// <summary>Fecha de retorno del servicio, si la tiene (dato informativo).</summary>
    public DateOnly? FechaRetorno { get; set; }
}

/// <summary>Datos mínimos del servicio del que sale la meta de kilometraje.</summary>
public class UltimoServicioConMetaDto
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public int KilometrajeServicio { get; set; }
}
