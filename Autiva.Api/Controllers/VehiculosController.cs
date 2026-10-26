using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints del expediente de vehículos del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiculosController : ControllerBase
{
    private readonly IVehiculoService _vehiculoService;

    public VehiculosController(IVehiculoService vehiculoService)
    {
        _vehiculoService = vehiculoService;
    }

    /// <summary>
    /// Registra un vehículo ligado al usuario del token.
    /// Si es el primer vehículo del usuario, queda como el activo/principal.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehiculoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarVehiculoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _vehiculoService.RegistrarAsync(solicitud, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.NoAutorizado)
                return Unauthorized(new { mensaje = resultado.Mensaje });

            if (resultado.Conflicto)
                return Conflict(new { mensaje = resultado.Mensaje });

            return BadRequest(new { mensaje = "Datos del vehículo inválidos.", errores = resultado.Errores });
        }

        return Created($"/api/vehiculos/{resultado.Datos!.Id}", resultado.Datos);
    }
}
