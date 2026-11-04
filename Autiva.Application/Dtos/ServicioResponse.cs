namespace Autiva.Application.Dtos;

/// <summary>
/// Datos de un servicio de mantenimiento devueltos al cliente.
/// </summary>
public class ServicioResponse
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public string TipoServicio { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public int KilometrajeServicio { get; set; }

    public decimal Costo { get; set; }

    public int? MetaProximoKm { get; set; }

    public DateOnly? FechaRetorno { get; set; }

    public string? Taller { get; set; }

    public string? Folio { get; set; }

    public string? Notas { get; set; }
}
