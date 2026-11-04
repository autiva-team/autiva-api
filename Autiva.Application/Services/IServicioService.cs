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

    /// <summary>Historial paginado (más reciente primero) de servicios de un vehículo del usuario del token.</summary>
    Task<ResultadoHistorialServicios> ObtenerHistorialAsync(
        Guid vehiculoId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);

    /// <summary>Detalle de un servicio de un vehículo del usuario del token.</summary>
    Task<ResultadoServicio> ObtenerDetalleAsync(
        Guid servicioId,
        CancellationToken cancellationToken = default);
}
