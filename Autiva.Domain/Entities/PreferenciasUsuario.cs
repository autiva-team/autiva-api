using System;
using System.Collections.Generic;

namespace Autiva.Domain.Entities;

/// <summary>
/// Ajustes del usuario (1:1 con usuario).
/// </summary>
public partial class PreferenciasUsuario
{
    public Guid UsuarioId { get; set; }

    public bool NotificacionesPush { get; set; }

    public string UnidadMedida { get; set; } = null!;

    public bool RespaldoNube { get; set; }

    public bool ModoOffline { get; set; }

    public DateTime? UltimaSincronizacion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
