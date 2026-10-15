using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Bitácora de servicios de mantenimiento.
/// </summary>
public partial class ServicioMantenimiento
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public byte TipoServicioId { get; set; }

    public DateOnly Fecha { get; set; }

    public int KilometrajeServicio { get; set; }

    public decimal Costo { get; set; }

    public int? MetaProximoKm { get; set; }

    public DateOnly? FechaRetorno { get; set; }

    public string? Taller { get; set; }

    public string? Folio { get; set; }

    public string? Notas { get; set; }

    public string? ComprobanteUrl { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual ICollection<Recordatorio> Recordatorio { get; set; } = new List<Recordatorio>();

    public virtual TipoServicio TipoServicio { get; set; } = null!;

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
