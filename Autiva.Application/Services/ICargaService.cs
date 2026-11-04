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

    /// <summary>Historial paginado (más reciente primero) de cargas de un vehículo del usuario del token.</summary>
    Task<ResultadoHistorialCargas> ObtenerHistorialAsync(
        Guid vehiculoId,
        int pagina,
        int tamano,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken cancellationToken = default);

    /// <summary>Detalle de una carga de un vehículo del usuario del token.</summary>
    Task<ResultadoCarga> ObtenerDetalleAsync(
        Guid cargaId,
        CancellationToken cancellationToken = default);
}
