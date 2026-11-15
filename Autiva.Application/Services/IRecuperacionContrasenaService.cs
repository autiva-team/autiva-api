namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Contrato del servicio de recuperación y restablecimiento de contraseña (HU-027).
/// </summary>
public interface IRecuperacionContrasenaService
{
    Task<ResultadoRecuperarContrasena> RecuperarAsync(
        RecuperarContrasenaRequest solicitud,
        CancellationToken cancellationToken = default);

    Task<ResultadoRestablecerContrasena> RestablecerAsync(
        RestablecerContrasenaRequest solicitud,
        CancellationToken cancellationToken = default);
}
