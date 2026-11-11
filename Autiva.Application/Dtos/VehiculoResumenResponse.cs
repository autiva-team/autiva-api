namespace Autiva.Application.Dtos;

/// <summary>
/// Resumen general del vehículo: contadores y datos del último registro.
/// </summary>
public class VehiculoResumenResponse
{
    public Guid Id { get; set; }
    public string Marca { get; set; } = null!;
    public string Modelo { get; set; } = null!;
    public short Anio { get; set; }
    public string Placas { get; set; } = null!;
    public string? Color { get; set; }
    public int OdometroActual { get; set; }
    public bool EsPrincipal { get; set; }
    public string? FotoUrl { get; set; }

    public int TotalCargas { get; set; }
    public DateTime? UltimaCarga { get; set; }

    public int TotalServicios { get; set; }
    public DateOnly? UltimoServicio { get; set; }

    public int TotalRecordatoriosPendientes { get; set; }
    public ProximoRecordatorioDto? ProximoRecordatorio { get; set; }
}

/// <summary>
/// Datos mínimos del próximo recordatorio pendiente.
/// </summary>
public class ProximoRecordatorioDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = null!;
    public DateOnly? FechaLimite { get; set; }
    public int? KmObjetivo { get; set; }
    public string Criterio { get; set; } = null!;
}
