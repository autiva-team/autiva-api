using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos de los tokens de recuperación de contraseña.
/// </summary>
public interface ITokenRecuperacionRepository
{
    /// <summary>
    /// Token por el hash de su valor, con el usuario incluido; null si no existe.
    /// </summary>
    Task<TokenRecuperacion?> ObtenerPorHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca como usados todos los tokens pendientes del usuario, para que solo
    /// sirva el último que se genere.
    /// </summary>
    Task InvalidarPendientesAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task AgregarAsync(TokenRecuperacion token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca el token como usado y guarda la nueva contraseña del usuario en una sola
    /// transacción.
    /// </summary>
    Task RestablecerContrasenaAsync(TokenRecuperacion token, Usuario usuario, CancellationToken cancellationToken = default);
}
