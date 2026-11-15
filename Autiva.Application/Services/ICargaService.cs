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

    /// <summary>
    /// Corrige una carga de un vehículo del usuario del token y recalcula el rendimiento
    /// de las cargas del vehículo (la editada y las que dependen de ella).
    /// </summary>
    Task<ResultadoCarga> EditarAsync(
        Guid cargaId,
        EditarCargaRequest solicitud,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina (borrado lógico) una carga de un vehículo del usuario del token y
    /// recalcula el rendimiento de las demás cargas del vehículo.
    /// </summary>
    Task<ResultadoEliminarCarga> EliminarAsync(
        Guid cargaId,
        CancellationToken cancellationToken = default);
}
