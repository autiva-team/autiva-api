namespace Autiva.Application.Services;

/// <summary>
/// Contrato del servicio de estadísticas de costo y eficiencia de un vehículo (HU-028).
/// </summary>
public interface IEstadisticasService
{
    Task<ResultadoEstadisticas> ObtenerAsync(
        Guid vehiculoId,
        string? periodo,
        CancellationToken cancellationToken = default);
}
