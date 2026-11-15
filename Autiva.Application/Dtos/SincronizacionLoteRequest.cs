namespace Autiva.Application.Dtos;

/// <summary>
/// Solicitud de sincronización en lote de cargas y servicios capturados sin conexión.
/// </summary>
public class SincronizacionLoteRequest
{
    public List<CargaSincronizacionDto> Cargas { get; set; } = new();
    public List<ServicioSincronizacionDto> Servicios { get; set; } = new();
}
