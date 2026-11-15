using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints del usuario autenticado (preferencias; la HU-030 agrega el perfil).
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : AutivaControllerBase
{
    private readonly IPreferenciasService _preferenciasService;

    public UsuariosController(IPreferenciasService preferenciasService)
    {
        _preferenciasService = preferenciasService;
    }

    /// <summary>Preferencias de la app del usuario autenticado.</summary>
    [HttpGet("me/preferencias")]
    [ProducesResponseType(typeof(PreferenciasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerPreferencias(CancellationToken cancellationToken)
    {
        var resultado = await _preferenciasService.ObtenerAsync(cancellationToken);

        if (!resultado.Exito)
            return ErrorDesdeResultado(resultado);

        return Ok(resultado.Datos);
    }

    /// <summary>Guarda las preferencias de la app del usuario autenticado.</summary>
    [HttpPut("me/preferencias")]
    [ProducesResponseType(typeof(PreferenciasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ActualizarPreferencias(
        [FromBody] ActualizarPreferenciasRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _preferenciasService.ActualizarAsync(solicitud, cancellationToken);

        if (!resultado.Exito)
            return ErrorDesdeResultado(resultado);

        return Ok(resultado.Datos);
    }
}
