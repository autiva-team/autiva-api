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

    public CargasController(ICargaService cargaService)
    {
        _cargaService = cargaService;
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
}
