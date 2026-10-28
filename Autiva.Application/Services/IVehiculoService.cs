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

    /// <summary>Lista los vehículos del usuario del token (vacía si no tiene ninguno).</summary>
    Task<ResultadoListaVehiculos> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene el detalle de un vehículo del usuario del token.</summary>
    Task<ResultadoVehiculoDetalle> ObtenerDetalleAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default);

    /// <summary>Marca un vehículo del usuario del token como el activo (es_principal).</summary>
    Task<ResultadoVehiculoDetalle> MarcarActivoAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default);
}
