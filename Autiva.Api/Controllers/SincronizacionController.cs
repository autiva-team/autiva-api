using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints para la sincronización de datos capturados sin conexión (offline).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SincronizacionController : ControllerBase
{
    private readonly ISincronizacionService _sincronizacionService;

    public SincronizacionController(ISincronizacionService sincronizacionService)
    {
        _sincronizacionService = sincronizacionService;
    }

    /// <summary>
    /// Recibe en lote cargas y servicios capturados en el cliente sin conexión a internet.
    /// Si un registro ya existe por su ID, se ignora de forma idempotente sin error.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SincronizacionLoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Sincronizar(
        [FromBody] SincronizacionLoteRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _sincronizacionService.SincronizarAsync(solicitud, cancellationToken);

        if (!resultado.Exito)
            return Unauthorized(new { mensaje = resultado.Mensaje });

        return Ok(resultado.Datos);
    }
}
