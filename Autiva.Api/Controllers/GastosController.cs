using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints de gastos de un vehículo del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/vehiculos/{vehiculoId:guid}/gastos")]
[Authorize]
public class GastosController : ControllerBase
{
    private readonly IGastoService _gastoService;

    public GastosController(IGastoService gastoService)
    {
        _gastoService = gastoService;
    }

    /// <summary>
    /// Resumen de gastos del mes: total, variación contra el mes anterior, desglose
    /// por categoría y costo promedio diario. Sin año/mes, usa el mes actual (hora de México).
    /// </summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenGastosResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerResumen(
        Guid vehiculoId,
        [FromQuery] int? anio = null,
        [FromQuery] int? mes = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _gastoService.ObtenerResumenAsync(vehiculoId, anio, mes, cancellationToken);

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
    /// Registra un gasto de otra categoría (lavado, peajes y estacionamiento, tenencia
    /// y trámites, multas, accesorios, otro).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GastoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        Guid vehiculoId,
        [FromBody] RegistrarGastoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _gastoService.RegistrarAsync(vehiculoId, solicitud, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            if (resultado.NoEncontrado)
                return NotFound(new { mensaje = resultado.Mensaje });

            if (resultado.Conflicto)
                return Conflict(new { mensaje = resultado.Mensaje });

            return BadRequest(new { mensaje = "Datos del gasto inválidos.", errores = resultado.Errores });
        }

        return Created($"/api/vehiculos/{vehiculoId}/gastos/{resultado.Datos!.Id}", resultado.Datos);
    }

    /// <summary>
    /// Historial de gastos registrados del vehículo, de más reciente a más antiguo,
    /// paginado y con filtros opcionales de mes (año y mes) y categoría.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<GastoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerHistorial(
        Guid vehiculoId,
        [FromQuery] int? anio = null,
        [FromQuery] int? mes = null,
        [FromQuery] byte? categoriaId = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 10,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _gastoService.ListarAsync(vehiculoId, anio, mes, categoriaId, pagina, tamano, cancellationToken);

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
}
