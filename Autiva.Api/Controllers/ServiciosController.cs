using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints de servicios de mantenimiento de un vehículo del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/vehiculos/{vehiculoId:guid}/servicios")]
[Authorize]
public class ServiciosController : ControllerBase
{
    private readonly IServicioService _servicioService;

    public ServiciosController(IServicioService servicioService)
    {
        _servicioService = servicioService;
    }

    /// <summary>
    /// Registra un servicio de mantenimiento del vehículo. Actualiza el odómetro del
    /// vehículo si el kilometraje del servicio es mayor al actual.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServicioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        Guid vehiculoId,
        [FromBody] RegistrarServicioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioService.RegistrarAsync(vehiculoId, solicitud, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            if (resultado.NoEncontrado)
                return NotFound(new { mensaje = resultado.Mensaje });

            if (resultado.Conflicto)
                return Conflict(new { mensaje = resultado.Mensaje });

            return BadRequest(new { mensaje = "Datos del servicio inválidos.", errores = resultado.Errores });
        }

        return Created($"/api/vehiculos/{vehiculoId}/servicios/{resultado.Datos!.Id}", resultado.Datos);
    }

    /// <summary>
    /// Historial de servicios de mantenimiento del vehículo, del más reciente al más antiguo,
    /// paginado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<ServicioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerHistorial(
        Guid vehiculoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 10,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _servicioService.ObtenerHistorialAsync(vehiculoId, pagina, tamano, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            return NotFound(new { mensaje = resultado.Mensaje });
        }

        return Ok(resultado.Datos);
    }

    /// <summary>
    /// Detalle de un servicio por id. Ruta independiente de vehiculoId porque el servicio ya
    /// identifica a su vehículo; la HU-025 (editar servicio) la reutilizará.
    /// </summary>
    [HttpGet("/api/servicios/{id:guid}")]
    [ProducesResponseType(typeof(ServicioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalle(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _servicioService.ObtenerDetalleAsync(id, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            return NotFound(new { mensaje = resultado.Mensaje });
        }

        return Ok(resultado.Datos);
    }
}
