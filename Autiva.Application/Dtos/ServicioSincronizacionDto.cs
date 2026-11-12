namespace Autiva.Application.Dtos;

/// <summary>
/// Servicio de mantenimiento registrado offline con ID generado en el cliente.
/// </summary>
public class ServicioSincronizacionDto
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public byte TipoServicioId { get; set; } = 1;
    public DateOnly Fecha { get; set; }
    public int KilometrajeServicio { get; set; }
    public decimal Costo { get; set; }
    public string? Taller { get; set; }
    public string? Folio { get; set; }
    public string? Notas { get; set; }
    public int? MetaProximoKm { get; set; }
    public DateOnly? FechaRetorno { get; set; }
}
