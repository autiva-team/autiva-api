using Autiva.Application.Dtos;
using Autiva.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints del usuario autenticado: perfil y preferencias de la app.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : AutivaControllerBase
{
    private readonly IPreferenciasService _preferenciasService;
    private readonly IPerfilService _perfilService;

    public UsuariosController(IPreferenciasService preferenciasService, IPerfilService perfilService)
    {
        _preferenciasService = preferenciasService;
        _perfilService = perfilService;
    }

    /// <summary>Datos del perfil del usuario autenticado.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PerfilResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerPerfil(CancellationToken cancellationToken)
    {
        var resultado = await _perfilService.ObtenerAsync(cancellationToken);

        if (!resultado.Exito)
            return ErrorDesdeResultado(resultado);

        return Ok(resultado.Datos);
    }

    /// <summary>Edita el nombre y el correo del usuario autenticado.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(PerfilResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditarPerfil(
        [FromBody] EditarPerfilRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _perfilService.EditarAsync(solicitud, cancellationToken);

        if (!resultado.Exito)
            return ErrorDesdeResultado(resultado, "Datos del perfil inválidos.");

        return Ok(resultado.Datos);
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
