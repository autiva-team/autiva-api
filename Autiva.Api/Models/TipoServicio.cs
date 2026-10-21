using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Catálogo de tipos de servicio de mantenimiento.
/// </summary>
public partial class TipoServicio
{
    public byte Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<ServicioMantenimiento> ServicioMantenimiento { get; set; } = new List<ServicioMantenimiento>();
}
