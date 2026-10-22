using System;
using System.Collections.Generic;

namespace Autiva.Domain.Entities;

/// <summary>
/// Historial de pólizas de seguro.
/// </summary>
public partial class PolizaSeguro
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public string Aseguradora { get; set; } = null!;

    public string NumeroPoliza { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public string? Cobertura { get; set; }

    public decimal? Deducible { get; set; }

    public decimal? Costo { get; set; }

    public string? TelefonoAsistencia { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual ICollection<Recordatorio> Recordatorio { get; set; } = new List<Recordatorio>();

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
