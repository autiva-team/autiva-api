namespace Autiva.Api.Services;

using Autiva.Api.Dtos;

/// <summary>
/// Contrato del servicio de cuentas de acceso.
/// </summary>
public interface IAuthService
{
    Task<ResultadoRegistro> RegistrarAsync(
        RegistroUsuarioRequest solicitud,
        CancellationToken cancellationToken = default);

    Task<ResultadoLogin> LoginAsync(
        LoginRequest solicitud,
        CancellationToken cancellationToken = default);
}
