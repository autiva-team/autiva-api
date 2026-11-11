namespace Autiva.Application.Dtos;

/// <summary>
/// Datos para crear un recordatorio por kilometraje.
/// </summary>
public class CrearRecordatorioKilometrajeRequest
{
    public string Titulo { get; set; } = null!;
    public string? Tipo { get; set; }
    public int KmObjetivo { get; set; }
    public byte AnticipacionDias { get; set; } = 7;
    public bool PushPrioritaria { get; set; } = false;
}
