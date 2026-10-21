using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Cuenta de acceso a la aplicación.
/// </summary>
public partial class Usuario
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string ContrasenaHash { get; set; } = null!;

    public string TipoCuenta { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public virtual ICollection<Notificacion> Notificacion { get; set; } = new List<Notificacion>();

    public virtual PreferenciasUsuario? PreferenciasUsuario { get; set; }

    public virtual ICollection<SuscripcionPush> SuscripcionPush { get; set; } = new List<SuscripcionPush>();

    public virtual Vehiculo? Vehiculo { get; set; }
}
