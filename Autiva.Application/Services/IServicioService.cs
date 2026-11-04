namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Contrato del servicio de mantenimiento.
/// </summary>
public interface IServicioService
{
    Task<ResultadoServicio> RegistrarAsync(
        Guid vehiculoId,
        RegistrarServicioRequest solicitud,
        CancellationToken cancellationToken = default);
}
