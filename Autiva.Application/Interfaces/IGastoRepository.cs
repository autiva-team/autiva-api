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

    /// <summary>Indica si ya existe un gasto con ese id (para evitar duplicados al sincronizar).</summary>
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene la categoría del catálogo <c>categoria_gasto</c>, o null si no existe.</summary>
    Task<CategoriaGasto?> ObtenerCategoriaAsync(byte categoriaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda el gasto. Devuelve <c>null</c> si el guardado falló por un conflicto de
    /// id duplicado (carrera sobre la llave primaria).
    /// </summary>
    Task<Gasto?> RegistrarAsync(Gasto gasto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial paginado (más reciente primero) de los gastos no eliminados del vehículo
    /// registrados en la tabla <c>gasto</c>, con filtros opcionales de mes (año y mes juntos)
    /// y categoría. Devuelve también el total de registros, para calcular el total de páginas.
    /// </summary>
    Task<(IReadOnlyList<Gasto> Items, int TotalRegistros)> ListarHistorialAsync(
        Guid vehiculoId, int pagina, int tamano, int? anio, int? mes, byte? categoriaId,
        CancellationToken cancellationToken = default);
}
