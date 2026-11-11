namespace Autiva.Application.Dtos;

/// <summary>
/// Datos para crear un recordatorio por fecha.
/// </summary>
public class CrearRecordatorioFechaRequest
{
    public string Titulo { get; set; } = null!;
    public string? Tipo { get; set; }
    public DateOnly FechaLimite { get; set; }
    public byte AnticipacionDias { get; set; } = 7;
    public bool PushPrioritaria { get; set; } = false;
}
