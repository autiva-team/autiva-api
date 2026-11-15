using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos de las preferencias de la app de un usuario.
/// </summary>
public interface IPreferenciasRepository
{
    /// <summary>Preferencias del usuario, o null si todavía no tiene fila.</summary>
    Task<PreferenciasUsuario?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task<PreferenciasUsuario> AgregarAsync(PreferenciasUsuario preferencias, CancellationToken cancellationToken = default);

    Task<PreferenciasUsuario> ActualizarAsync(PreferenciasUsuario preferencias, CancellationToken cancellationToken = default);
}
