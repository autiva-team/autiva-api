namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Contrato del servicio de vehículos.
/// </summary>
public interface IVehiculoService
{
    Task<ResultadoVehiculo> RegistrarAsync(
        RegistrarVehiculoRequest solicitud,
        CancellationToken cancellationToken = default);
}
