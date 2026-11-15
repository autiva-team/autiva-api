using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Estadísticas de costo y eficiencia de un vehículo del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/vehiculos/{vehiculoId:guid}/estadisticas")]
[Authorize]
public class EstadisticasController : AutivaControllerBase
{
    private readonly IEstadisticasService _estadisticasService;

    public EstadisticasController(IEstadisticasService estadisticasService)
    {
        _estadisticasService = estadisticasService;
    }

    /// <summary>
    /// Gasto total, distancia, costo por km y rendimiento del vehículo en el periodo
    /// (1M, 3M, 6M o 1A; por defecto 1M).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(EstadisticasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(
        Guid vehiculoId,
        [FromQuery] string? periodo = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _estadisticasService.ObtenerAsync(vehiculoId, periodo, cancellationToken);

        if (!resultado.Exito)
            return ErrorDesdeResultado(resultado);

        return Ok(resultado.Datos);
    }
}
