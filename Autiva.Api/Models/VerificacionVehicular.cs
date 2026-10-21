using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Historial de verificaciones ambientales.
/// </summary>
public partial class VerificacionVehicular
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public string Holograma { get; set; } = null!;

    public string? Engomado { get; set; }

    public string? Periodo { get; set; }

    public DateOnly? FechaVerificacion { get; set; }

    public DateOnly? ProximaFecha { get; set; }

    public string? Verificentro { get; set; }

    public decimal? Costo { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual ICollection<Recordatorio> Recordatorio { get; set; } = new List<Recordatorio>();

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
