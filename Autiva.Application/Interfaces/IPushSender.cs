using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Despachador de notificaciones Web Push al navegador del cliente.
/// </summary>
public interface IPushSender
{
    string ObtenerClavePublica();

    Task<bool> EnviarPushAsync(
        SuscripcionPush suscripcion,
        string titulo,
        string mensaje,
        object? payload = null,
        CancellationToken cancellationToken = default);
}
