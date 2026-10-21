using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

public partial class VwGastosConsolidados
{
    public Guid VehiculoId { get; set; }

    public string Categoria { get; set; } = null!;

    public string? Concepto { get; set; }

    public decimal? Monto { get; set; }

    public DateOnly? Fecha { get; set; }

    public int? Odometro { get; set; }

    public string Origen { get; set; } = null!;

    public Guid OrigenId { get; set; }
}
