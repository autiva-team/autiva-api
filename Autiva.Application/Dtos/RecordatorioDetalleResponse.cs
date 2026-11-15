namespace Autiva.Application.Dtos;

/// <summary>
/// Información detallada de un recordatorio con cálculos de vencimiento.
/// </summary>
public class RecordatorioDetalleResponse
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public string? VehiculoPlacas { get; set; }
    public string? VehiculoModelo { get; set; }
    public string Tipo { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string Criterio { get; set; } = null!;
    public DateOnly? FechaLimite { get; set; }
    public int? KmObjetivo { get; set; }
    public byte AnticipacionDias { get; set; }
    public bool PushPrioritaria { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime? FechaConcluido { get; set; }
    public DateTime CreadoEn { get; set; }

    /// <summary>Indica si el recordatorio ya pasó su fecha límite o kilometraje objetivo.</summary>
    public bool EstaVencido { get; set; }

    /// <summary>Días restantes para vencer (negativo si ya venció, null si es por kilometraje).</summary>
    public int? DiasRestantes { get; set; }

    /// <summary>Kilómetros restantes para alcanzar el objetivo (null si es por fecha).</summary>
    public int? KmRestantes { get; set; }

    /// <summary>Indica si el recordatorio está dentro del periodo de aviso previo.</summary>
    public bool ProximoAVencer { get; set; }
}
