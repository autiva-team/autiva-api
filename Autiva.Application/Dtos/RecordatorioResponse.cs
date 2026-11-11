namespace Autiva.Application.Dtos;

/// <summary>
/// Datos de un recordatorio devueltos al cliente.
/// </summary>
public class RecordatorioResponse
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public string Tipo { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string Criterio { get; set; } = null!;
    public DateOnly? FechaLimite { get; set; }
    public int? KmObjetivo { get; set; }
    public byte AnticipacionDias { get; set; }
    public bool PushPrioritaria { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime CreadoEn { get; set; }
}
