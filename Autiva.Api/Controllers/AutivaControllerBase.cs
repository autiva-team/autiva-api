using Autiva.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Traduce un resultado de servicio (patrón ResultadoX) al código HTTP de error que le
/// corresponde, para no repetir la misma cadena de ifs en cada controlador: 401 si no
/// está autorizado, 404 si no se encontró, 409 si hay conflicto, 400 si es un error de
/// validación con un solo mensaje, o 400 con la lista de errores en cualquier otro caso.
/// </summary>
public abstract class AutivaControllerBase : ControllerBase
{
    protected IActionResult ErrorDesdeResultado(IResultadoOperacion resultado, string mensajeInvalido = "")
    {
        if (resultado.NoAutorizado)
            return Unauthorized(new { mensaje = resultado.Mensaje });

        if (resultado.NoEncontrado)
            return NotFound(new { mensaje = resultado.Mensaje });

        if (resultado.Conflicto)
            return Conflict(new { mensaje = resultado.Mensaje });

        if (resultado.Validacion)
            return BadRequest(new { mensaje = resultado.Mensaje });

        return BadRequest(new { mensaje = mensajeInvalido, errores = resultado.Errores });
    }
}
