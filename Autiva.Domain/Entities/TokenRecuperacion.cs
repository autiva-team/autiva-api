using System;

namespace Autiva.Domain.Entities;

/// <summary>
/// Token de un solo uso para restablecer la contraseña (HU-027).
/// </summary>
public partial class TokenRecuperacion
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiraEn { get; set; }

    public DateTime? UsadoEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
