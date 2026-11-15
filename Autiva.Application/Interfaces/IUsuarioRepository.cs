using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos de cuentas de usuario, sin exponer detalles de EF Core.
/// </summary>
public interface IUsuarioRepository
{
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default);

    /// <summary>Usuario por id, o null si ya no existe.</summary>
    Task<Usuario?> ObtenerPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si otro usuario (distinto a <paramref name="usuarioIdExcluido"/>) ya tiene
    /// ese correo. El correo propio no cuenta como repetido.
    /// </summary>
    Task<bool> ExisteCorreoEnOtroUsuarioAsync(string correo, Guid usuarioIdExcluido, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega el usuario y guarda los cambios. Devuelve <c>false</c> si el guardado
    /// falló por un conflicto de correo duplicado (carrera sobre el índice único).
    /// </summary>
    Task<bool> AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>Edita el nombre y el correo del usuario, y actualiza <c>actualizado_en</c>.</summary>
    Task<Usuario> EditarPerfilAsync(Guid usuarioId, string nombre, string correo, CancellationToken cancellationToken = default);
}
