using System;
using System.Collections.Generic;

namespace Autiva.Domain.Entities;

/// <summary>
/// Recordatorios por fecha o kilometraje.
/// </summary>
public partial class Recordatorio
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public string Tipo { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string Criterio { get; set; } = null!;

    public DateOnly? FechaLimite { get; set; }

    public int? KmObjetivo { get; set; }

    public byte AnticipacionDias { get; set; }

    public bool PushPrioritaria { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? FechaConcluido { get; set; }

    public Guid? PolizaId { get; set; }

    public Guid? VerificacionId { get; set; }

    public Guid? ServicioId { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual ICollection<Notificacion> Notificacion { get; set; } = new List<Notificacion>();

    public virtual PolizaSeguro? Poliza { get; set; }

    public virtual ICollection<RecordatorioCanal> RecordatorioCanal { get; set; } = new List<RecordatorioCanal>();

    public virtual ServicioMantenimiento? Servicio { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;

    public virtual VerificacionVehicular? Verificacion { get; set; }
}
