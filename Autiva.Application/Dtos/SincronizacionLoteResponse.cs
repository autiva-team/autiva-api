namespace Autiva.Application.Dtos;

/// <summary>
/// Resumen de los elementos procesados en la sincronización en lote.
/// </summary>
public class SincronizacionLoteResponse
{
    public int CargasRecibidas { get; set; }
    public int CargasInsertadas { get; set; }
    public int CargasIgnoradasDuplicadas { get; set; }

    public int ServiciosRecibidos { get; set; }
    public int ServiciosInsertados { get; set; }
    public int ServiciosIgnoradosDuplicados { get; set; }

    public List<Guid> CargasSincronizadasIds { get; set; } = new();
    public List<Guid> ServiciosSincronizadosIds { get; set; } = new();
}
