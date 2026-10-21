using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Gastos generales sin tabla propia.
/// </summary>
public partial class Gasto
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public byte CategoriaId { get; set; }

    public string Concepto { get; set; } = null!;

    public decimal Monto { get; set; }

    public DateOnly Fecha { get; set; }

    public int? Odometro { get; set; }

    public string? ComprobanteUrl { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual CategoriaGasto Categoria { get; set; } = null!;

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
