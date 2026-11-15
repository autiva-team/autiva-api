using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso de solo lectura a la vista vw_gastos_consolidados (cargas, servicios,
/// pólizas, verificaciones y gastos generales).
/// </summary>
public interface IGastoRepository
{
    /// <summary>
    /// Gastos consolidados del vehículo con fecha entre <paramref name="desde"/> y
    /// <paramref name="hasta"/> (ambas inclusive).
    /// </summary>
    Task<IReadOnlyList<VwGastosConsolidados>> ListarPorVehiculoYRangoAsync(
        Guid vehiculoId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);
}
