using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints para suscripciones a notificaciones Push y consulta de la bandeja de alertas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly IPushService _pushService;

    public PushController(IPushService pushService)
    {
        _pushService = pushService;
    }

    /// <summary>
    /// Obtiene la clave pública VAPID configurada en el servidor para generar la suscripción en el navegador.
    /// </summary>
    [HttpGet("clave-publica")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClavePublicaVapidResponse), StatusCodes.Status200OK)]
    public IActionResult ObtenerClavePublica()
    {
        var clave = _pushService.ObtenerClavePublica();
        return Ok(clave);
    }

    /// <summary>
    /// Registra o renueva una suscripción Web Push asociada al usuario autenticado.
    /// </summary>
    [HttpPost("suscribir")]
    [ProducesResponseType(typeof(SuscripcionPushResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Suscribir(
        [FromBody] SuscripcionPushRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _pushService.SuscribirAsync(solicitud, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            return BadRequest(new { errores = resultado.Errores });
        }

        return Ok(resultado.Datos);
    }

    /// <summary>
    /// Desactiva una suscripción Web Push existente.
    /// </summary>
    [HttpPost("desuscribir")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Desuscribir(
        [FromBody] DesuscribirPushRequest solicitud,
        CancellationToken cancellationToken)
    {
        await _pushService.DesuscribirAsync(solicitud.Endpoint, cancellationToken);
        return Ok(new { mensaje = "Suscripción desactivada correctamente." });
    }

    /// <summary>
    /// Consulta las notificaciones recibidas en la bandeja del usuario.
    /// </summary>
    [HttpGet("notificaciones")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerNotificaciones(CancellationToken cancellationToken)
    {
        var notificaciones = await _pushService.ObtenerNotificacionesAsync(cancellationToken);
        return Ok(notificaciones);
    }

    /// <summary>
    /// Marca una notificación específica como leída.
    /// </summary>
    [HttpPatch("notificaciones/{id:guid}/leida")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken cancellationToken)
    {
        var exito = await _pushService.MarcarNotificacionLeidaAsync(id, cancellationToken);
        if (!exito)
            return NotFound(new { mensaje = "No se encontró la notificación especificada." });

        return Ok(new { mensaje = "Notificación marcada como leída." });
    }
}

public class DesuscribirPushRequest
{
    public string Endpoint { get; set; } = null!;
}
