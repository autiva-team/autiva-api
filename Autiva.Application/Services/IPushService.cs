using Autiva.Application.Dtos;
using Autiva.Application.Services;

namespace Autiva.Application.Services;

/// <summary>
/// Contrato de servicio para suscripciones Push, bandeja de notificaciones
/// y envío de alertas al usuario.
/// </summary>
public interface IPushService
{
    ClavePublicaVapidResponse ObtenerClavePublica();

    Task<ResultadoPush> SuscribirAsync(
        SuscripcionPushRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DesuscribirAsync(
        string endpoint,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificacionResponse>> ObtenerNotificacionesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> MarcarNotificacionLeidaAsync(
        Guid notificacionId,
        CancellationToken cancellationToken = default);

    Task EnviarNotificacionUsuarioAsync(
        Guid usuarioId,
        string titulo,
        string mensaje,
        Guid? recordatorioId = null,
        CancellationToken cancellationToken = default);
}
