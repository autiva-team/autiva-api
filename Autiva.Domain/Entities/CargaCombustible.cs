using System;
using System.Collections.Generic;

namespace Autiva.Domain.Entities;

/// <summary>
/// Cargas de combustible; captura principal sin conexión.
/// </summary>
public partial class CargaCombustible
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public byte TipoCombustibleId { get; set; }

    public DateTime FechaHora { get; set; }

    public decimal Litros { get; set; }

    public decimal CostoTotal { get; set; }

    public int Odometro { get; set; }

    public bool TanqueLleno { get; set; }

    public decimal? RendimientoKml { get; set; }

    public string? Gasolinera { get; set; }

    public string? Folio { get; set; }

    public string? Notas { get; set; }

    public string? ComprobanteUrl { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual TipoCombustible TipoCombustible { get; set; } = null!;

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
