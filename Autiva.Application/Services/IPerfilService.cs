namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Contrato del servicio del perfil del usuario autenticado (HU-030).
/// </summary>
public interface IPerfilService
{
    Task<ResultadoPerfil> ObtenerAsync(CancellationToken cancellationToken = default);

    Task<ResultadoPerfil> EditarAsync(
        EditarPerfilRequest solicitud,
        CancellationToken cancellationToken = default);
}
