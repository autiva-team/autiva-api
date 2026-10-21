using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Bandeja de notificaciones del usuario.
/// </summary>
public partial class Notificacion
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid? RecordatorioId { get; set; }

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public DateTime FechaEnvio { get; set; }

    public bool Leida { get; set; }

    public virtual Recordatorio? Recordatorio { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
