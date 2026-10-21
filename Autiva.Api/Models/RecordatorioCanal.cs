using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Canales de notificación de cada recordatorio.
/// </summary>
public partial class RecordatorioCanal
{
    public Guid RecordatorioId { get; set; }

    public string Canal { get; set; } = null!;

    public virtual Recordatorio Recordatorio { get; set; } = null!;
}
