using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos de cuentas de usuario, sin exponer detalles de EF Core.
/// </summary>
public interface IUsuarioRepository
{
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega el usuario y guarda los cambios. Devuelve <c>false</c> si el guardado
    /// falló por un conflicto de correo duplicado (carrera sobre el índice único).
    /// </summary>
    Task<bool> AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
