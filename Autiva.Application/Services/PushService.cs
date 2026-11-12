using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Implementación de la gestión de suscripciones Web Push, bandeja de notificaciones
/// y despacho de alertas al dispositivo del usuario.
/// </summary>
public class PushService : IPushService
{
    private readonly IPushRepository _pushRepository;
    private readonly IPushSender _pushSender;
    private readonly ICurrentUserService _currentUserService;

    public PushService(
        IPushRepository pushRepository,
        IPushSender pushSender,
        ICurrentUserService currentUserService)
    {
        _pushRepository = pushRepository;
        _pushSender = pushSender;
        _currentUserService = currentUserService;
    }

    public ClavePublicaVapidResponse ObtenerClavePublica()
    {
        return new ClavePublicaVapidResponse
        {
            ClavePublica = _pushSender.ObtenerClavePublica()
        };
    }

    public async Task<ResultadoPush> SuscribirAsync(
        SuscripcionPushRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;
        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoPush.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var errores = ValidarSuscripcion(request);
        if (errores.Count > 0)
            return ResultadoPush.CrearValidacion(errores);

        var endpoint = request.Endpoint.Trim();
        var existente = await _pushRepository.ObtenerPorEndpointAsync(endpoint, cancellationToken);

        SuscripcionPush suscripcion;
        if (existente is not null)
        {
            existente.UsuarioId = usuarioId.Value;
            existente.ClaveP256dh = request.ClaveP256dh.Trim();
            existente.ClaveAuth = request.ClaveAuth.Trim();
            existente.Dispositivo = request.Dispositivo?.Trim();
            existente.Activa = true;
            suscripcion = await _pushRepository.GuardarSuscripcionAsync(existente, cancellationToken);
        }
        else
        {
            suscripcion = new SuscripcionPush
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId.Value,
                Endpoint = endpoint,
                ClaveP256dh = request.ClaveP256dh.Trim(),
                ClaveAuth = request.ClaveAuth.Trim(),
                Dispositivo = request.Dispositivo?.Trim(),
                Activa = true,
                CreadoEn = DateTime.UtcNow
            };
            suscripcion = await _pushRepository.GuardarSuscripcionAsync(suscripcion, cancellationToken);
        }

        return ResultadoPush.CrearExito(new SuscripcionPushResponse
        {
            Id = suscripcion.Id,
            Endpoint = suscripcion.Endpoint,
            Dispositivo = suscripcion.Dispositivo,
            Activa = suscripcion.Activa,
            CreadoEn = suscripcion.CreadoEn
        });
    }

    public async Task<bool> DesuscribirAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return false;

        await _pushRepository.DesactivarSuscripcionAsync(endpoint.Trim(), cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<NotificacionResponse>> ObtenerNotificacionesAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;
        if (usuarioId is null || usuarioId == Guid.Empty)
            return Array.Empty<NotificacionResponse>();

        var notificaciones = await _pushRepository.ObtenerNotificacionesUsuarioAsync(usuarioId.Value, cancellationToken);

        return notificaciones
            .Select(n => new NotificacionResponse
            {
                Id = n.Id,
                RecordatorioId = n.RecordatorioId,
                Titulo = n.Titulo,
                Mensaje = n.Mensaje,
                FechaEnvio = n.FechaEnvio,
                Leida = n.Leida
            })
            .ToList();
    }

    public async Task<bool> MarcarNotificacionLeidaAsync(
        Guid notificacionId,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;
        if (usuarioId is null || usuarioId == Guid.Empty)
            return false;

        var notificacion = await _pushRepository.ObtenerNotificacionPorIdAsync(notificacionId, usuarioId.Value, cancellationToken);
        if (notificacion is null)
            return false;

        await _pushRepository.MarcarNotificacionLeidaAsync(notificacionId, usuarioId.Value, cancellationToken);
        return true;
    }

    public async Task EnviarNotificacionUsuarioAsync(
        Guid usuarioId,
        string titulo,
        string mensaje,
        Guid? recordatorioId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Registrar notificación en la bandeja persistente
        var notificacion = new Notificacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            RecordatorioId = recordatorioId,
            Titulo = titulo,
            Mensaje = mensaje,
            FechaEnvio = DateTime.UtcNow,
            Leida = false
        };

        await _pushRepository.GuardarNotificacionAsync(notificacion, cancellationToken);

        // 2. Enviar a cada dispositivo activo del usuario
        var suscripciones = await _pushRepository.ObtenerActivasPorUsuarioAsync(usuarioId, cancellationToken);

        foreach (var sub in suscripciones)
        {
            var enviado = await _pushSender.EnviarPushAsync(
                sub,
                titulo,
                mensaje,
                payload: new { notificacionId = notificacion.Id, recordatorioId },
                cancellationToken);

            // Si el endpoint falló de forma irrecuperable (ej. 410 Gone / 404), desactivar
            if (!enviado)
            {
                await _pushRepository.DesactivarSuscripcionAsync(sub.Endpoint, cancellationToken);
            }
        }
    }

    private static List<string> ValidarSuscripcion(SuscripcionPushRequest request)
    {
        var errores = new List<string>();

        if (request is null)
        {
            errores.Add("La solicitud de suscripción es obligatoria.");
            return errores;
        }

        if (string.IsNullOrWhiteSpace(request.Endpoint) || !Uri.TryCreate(request.Endpoint, UriKind.Absolute, out _))
            errores.Add("El endpoint de la suscripción debe ser una URL válida.");

        if (string.IsNullOrWhiteSpace(request.ClaveP256dh))
            errores.Add("La clave pública p256dh es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.ClaveAuth))
            errores.Add("El secreto de autenticación auth es obligatorio.");

        return errores;
    }
}
