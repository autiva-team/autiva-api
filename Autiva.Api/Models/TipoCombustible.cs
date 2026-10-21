using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Catálogo de tipos de combustible.
/// </summary>
public partial class TipoCombustible
{
    public byte Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<CargaCombustible> CargaCombustible { get; set; } = new List<CargaCombustible>();
}
