using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos para suscripciones Web Push y bandeja de notificaciones.
/// </summary>
public interface IPushRepository
{
    Task<SuscripcionPush?> ObtenerPorEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SuscripcionPush>> ObtenerActivasPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<SuscripcionPush> GuardarSuscripcionAsync(SuscripcionPush suscripcion, CancellationToken cancellationToken = default);
    Task DesactivarSuscripcionAsync(string endpoint, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notificacion>> ObtenerNotificacionesUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<Notificacion?> ObtenerNotificacionPorIdAsync(Guid notificacionId, Guid usuarioId, CancellationToken cancellationToken = default);
    Task GuardarNotificacionAsync(Notificacion notificacion, CancellationToken cancellationToken = default);
    Task MarcarNotificacionLeidaAsync(Guid notificacionId, Guid usuarioId, CancellationToken cancellationToken = default);
}
