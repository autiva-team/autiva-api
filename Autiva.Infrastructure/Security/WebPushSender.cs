using System.Net;
using System.Text.Json;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace Autiva.Infrastructure.Security;

/// <summary>
/// Despachador de notificaciones Web Push a través de la librería WebPush y protocolo RFC 8292.
/// </summary>
public class WebPushSender : IPushSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(IConfiguration configuration, ILogger<WebPushSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string ObtenerClavePublica()
    {
        return _configuration["Vapid:PublicKey"] ?? string.Empty;
    }

    public async Task<bool> EnviarPushAsync(
        SuscripcionPush suscripcion,
        string titulo,
        string mensaje,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var subject = _configuration["Vapid:Subject"] ?? "mailto:contacto@autiva.com";
        var publicKey = _configuration["Vapid:PublicKey"];
        var privateKey = _configuration["Vapid:PrivateKey"];

        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
        {
            _logger.LogWarning("VAPID keys no configuradas en Vapid:PublicKey/Vapid:PrivateKey. No se puede despachar Web Push.");
            return true;
        }

        try
        {
            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            var pushSubscription = new PushSubscription(suscripcion.Endpoint, suscripcion.ClaveP256dh, suscripcion.ClaveAuth);
            var client = new WebPushClient();

            var contenido = JsonSerializer.Serialize(new
            {
                notification = new
                {
                    title = titulo,
                    body = mensaje,
                    icon = "/icon-192x192.png",
                    badge = "/icon-192x192.png",
                    data = payload
                }
            });

            await client.SendNotificationAsync(pushSubscription, contenido, vapidDetails, cancellationToken);
            return true;
        }
        catch (WebPushException ex)
        {
            _logger.LogWarning(ex, "Error WebPush al endpoint {Endpoint}: HTTP {StatusCode}", suscripcion.Endpoint, ex.StatusCode);

            if (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado despachando Web Push al endpoint {Endpoint}", suscripcion.Endpoint);
            return true;
        }
    }
}
