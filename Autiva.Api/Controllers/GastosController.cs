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
}
