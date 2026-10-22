using System;
using System.Collections.Generic;

namespace Autiva.Domain.Entities;

/// <summary>
/// Suscripciones Web Push por dispositivo.
/// </summary>
public partial class SuscripcionPush
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string Endpoint { get; set; } = null!;

    public string ClaveP256dh { get; set; } = null!;

    public string ClaveAuth { get; set; } = null!;

    public string? Dispositivo { get; set; }

    public bool Activa { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
