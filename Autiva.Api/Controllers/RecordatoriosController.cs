using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints para el resumen del vehículo y la gestión de recordatorios.
/// </summary>
[ApiController]
[Route("api/vehiculos/{vehiculoId:guid}")]
[Authorize]
public class RecordatoriosController : ControllerBase
{
    private readonly IRecordatorioService _recordatorioService;

    public RecordatoriosController(IRecordatorioService recordatorioService)
    {
        _recordatorioService = recordatorioService;
    }

    /// <summary>
    /// Devuelve el resumen general del vehículo: contadores de cargas, servicios
    /// y recordatorios pendientes, más el próximo recordatorio activo.
    /// </summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(VehiculoResumenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerResumen(Guid vehiculoId, CancellationToken cancellationToken)
    {
        var resultado = await _recordatorioService.ObtenerResumenAsync(vehiculoId, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            return NotFound(new { mensaje = resultado.Mensaje });
        }

        return Ok(resultado.Datos);
    }

    /// <summary>
    /// Crea un recordatorio basado en una fecha límite para el vehículo del token.
    /// </summary>
    [HttpPost("recordatorios/fecha")]
    [ProducesResponseType(typeof(RecordatorioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CrearPorFecha(
        Guid vehiculoId,
        [FromBody] CrearRecordatorioFechaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _recordatorioService.CrearPorFechaAsync(vehiculoId, solicitud, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            if (resultado.NoEncontrado)
                return NotFound(new { mensaje = resultado.Mensaje });

            return BadRequest(new { mensaje = "Datos del recordatorio inválidos.", errores = resultado.Errores });
        }

        return Created($"/api/vehiculos/{vehiculoId}/recordatorios/{resultado.Datos!.Id}", resultado.Datos);
    }

    /// <summary>
    /// Crea un recordatorio basado en un objetivo de kilometraje para el vehículo del token.
    /// </summary>
    [HttpPost("recordatorios/kilometraje")]
    [ProducesResponseType(typeof(RecordatorioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CrearPorKilometraje(
        Guid vehiculoId,
        [FromBody] CrearRecordatorioKilometrajeRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _recordatorioService.CrearPorKilometrajeAsync(vehiculoId, solicitud, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            if (resultado.NoEncontrado)
                return NotFound(new { mensaje = resultado.Mensaje });

            return BadRequest(new { mensaje = "Datos del recordatorio inválidos.", errores = resultado.Errores });
        }

        return Created($"/api/vehiculos/{vehiculoId}/recordatorios/{resultado.Datos!.Id}", resultado.Datos);
    }
}
