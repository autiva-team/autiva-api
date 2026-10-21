using System.Net.Mail;
using Autiva.Api.Dtos;
using Autiva.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers;

/// <summary>
/// Endpoints de cuentas de acceso.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registra una cuenta nueva en la tabla <c>usuario</c>.
    /// La contraseña se guarda hasheada con BCrypt y el tipo de cuenta
    /// siempre se crea como "estandar".
    /// </summary>
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(RegistroUsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistroUsuarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        solicitud ??= new RegistroUsuarioRequest();

        var nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        var correo = solicitud.Correo?.Trim().ToLowerInvariant() ?? string.Empty;
        var contrasena = solicitud.Contrasena ?? string.Empty;

        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(nombre))
            errores.Add("El nombre es obligatorio.");
        else if (nombre.Length > 120)
            errores.Add("El nombre no puede superar los 120 caracteres.");

        if (string.IsNullOrWhiteSpace(correo))
            errores.Add("El correo es obligatorio.");
        else if (correo.Length > 150)
            errores.Add("El correo no puede superar los 150 caracteres.");
        else if (!MailAddress.TryCreate(correo, out _))
            errores.Add("El correo no tiene un formato válido.");

        if (string.IsNullOrWhiteSpace(contrasena))
            errores.Add("La contraseña es obligatoria.");
        else if (contrasena.Length < 8)
            errores.Add("La contraseña debe tener al menos 8 caracteres.");
        else if (contrasena.Length > 72)
            errores.Add("La contraseña no puede superar los 72 caracteres (límite de BCrypt).");

        if (errores.Count > 0)
            return BadRequest(new { mensaje = "Datos de registro inválidos.", errores });

        var resultado = await _authService.RegistrarAsync(solicitud, cancellationToken);

        if (!resultado.Exito)
        {
            if (resultado.Conflicto)
                return Conflict(new { mensaje = resultado.Mensaje });

            return BadRequest(new { mensaje = "Datos de registro inválidos." });
        }

        return Created("/api/auth/registrar", resultado.Datos);
    }
}
