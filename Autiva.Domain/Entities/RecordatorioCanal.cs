using System;
using System.Collections.Generic;

namespace Autiva.Domain.Entities;

/// <summary>
/// Canales de notificación de cada recordatorio.
/// </summary>
public partial class RecordatorioCanal
{
    public Guid RecordatorioId { get; set; }

    public string Canal { get; set; } = null!;

    public virtual Recordatorio Recordatorio { get; set; } = null!;
}
