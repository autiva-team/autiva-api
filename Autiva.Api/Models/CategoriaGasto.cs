using System;
using System.Collections.Generic;

namespace Autiva.Api.Models;

/// <summary>
/// Catálogo de categorías de gastos generales.
/// </summary>
public partial class CategoriaGasto
{
    public byte Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Gasto> Gasto { get; set; } = new List<Gasto>();
}
