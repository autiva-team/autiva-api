using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints de cargas de combustible de un vehículo del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/vehiculos/{vehiculoId:guid}/cargas")]
[Authorize]
public class CargasController : ControllerBase
{
    private readonly ICargaService _cargaService;
    private readonly IRendimientoService _rendimientoService;

    public CargasController(ICargaService cargaService, IRendimientoService rendimientoService)
    {
        _cargaService = cargaService;
        _rendimientoService = rendimientoService;
    }

    /// <summary>
    /// Registra una carga de combustible del vehículo. Calcula precio por litro,
    /// km recorridos y rendimiento, y actualiza el odómetro del vehículo si corresponde.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CargaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        Guid vehiculoId,
        [FromBody] RegistrarCargaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _cargaService.RegistrarAsync(vehiculoId, solicitud, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            if (resultado.NoEncontrado)
                return NotFound(new { mensaje = resultado.Mensaje });

            if (resultado.Conflicto)
                return Conflict(new { mensaje = resultado.Mensaje });

            return BadRequest(new { mensaje = "Datos de la carga inválidos.", errores = resultado.Errores });
        }

        return Created($"/api/vehiculos/{vehiculoId}/cargas/{resultado.Datos!.Id}", resultado.Datos);
    }

    /// <summary>
    /// Historial de cargas del vehículo, de la más reciente a la más antigua, paginado y con
    /// filtro opcional de fechas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<CargaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerHistorial(
        Guid vehiculoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 10,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _cargaService.ObtenerHistorialAsync(vehiculoId, pagina, tamano, desde, hasta, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            if (resultado.Validacion)
                return BadRequest(new { mensaje = resultado.Mensaje });

            return NotFound(new { mensaje = resultado.Mensaje });
        }

        return Ok(resultado.Datos);
    }

    /// <summary>
    /// Detalle de una carga por id. Ruta independiente de vehiculoId porque la carga ya
    /// identifica a su vehículo; la HU-023 (editar carga) la reutilizará.
    /// </summary>
    [HttpGet("/api/cargas/{id:guid}")]
    [ProducesResponseType(typeof(CargaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalle(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _cargaService.ObtenerDetalleAsync(id, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            return NotFound(new { mensaje = resultado.Mensaje });
        }

        return Ok(resultado.Datos);
    }

    /// <summary>
    /// Rendimiento (km/L) y gasto de combustible del vehículo: promedio general,
    /// variación contra el mes anterior, gasto del mes actual y autonomía estimada.
    /// </summary>
    [HttpGet("rendimiento")]
    [ProducesResponseType(typeof(RendimientoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerRendimiento(Guid vehiculoId, CancellationToken cancellationToken)
    {
        var resultado = await _rendimientoService.ObtenerAsync(vehiculoId, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            return NotFound(new { mensaje = resultado.Mensaje });
        }

        return Ok(resultado.Datos);
    }
}
