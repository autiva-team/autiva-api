namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para editar un servicio de mantenimiento. Mismos campos que el
/// registro, sin id (el servicio ya existe).
/// </summary>
public class EditarServicioRequest
{
    /// <summary>Catálogo <c>tipo_servicio</c>.</summary>
    public byte TipoServicioId { get; set; }

    /// <summary>Fecha en que se hizo el servicio.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Kilometraje del vehículo al momento del servicio.</summary>
    public int KilometrajeServicio { get; set; }

    /// <summary>Costo del servicio (puede ser 0, por ejemplo en garantía).</summary>
    public decimal Costo { get; set; }

    /// <summary>Kilometraje sugerido para el próximo servicio de este tipo (opcional).</summary>
    public int? MetaProximoKm { get; set; }

    /// <summary>Fecha en que se recoge el vehículo, si se dejó en el taller (opcional).</summary>
    public DateOnly? FechaRetorno { get; set; }

    /// <summary>Nombre del taller (opcional, máx. 120).</summary>
    public string? Taller { get; set; }

    /// <summary>Folio del ticket o factura (opcional, máx. 50).</summary>
    public string? Folio { get; set; }

    /// <summary>Notas libres (opcional, máx. 500).</summary>
    public string? Notas { get; set; }
}
