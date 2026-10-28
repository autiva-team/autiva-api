namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Contrato del servicio de cargas de combustible.
/// </summary>
public interface ICargaService
{
    Task<ResultadoCarga> RegistrarAsync(
        Guid vehiculoId,
        RegistrarCargaRequest solicitud,
        CancellationToken cancellationToken = default);
}
