using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos de servicios de mantenimiento, sin exponer detalles de EF Core.
/// </summary>
public interface IServicioRepository
{
    /// <summary>Indica si ya existe un servicio con ese id (para evitar duplicados al sincronizar).</summary>
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el tipo de servicio del catálogo, o null si no existe.</summary>
    Task<TipoServicio?> ObtenerTipoServicioAsync(byte tipoServicioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda el servicio y, si su kilometraje es mayor al actual del vehículo, actualiza
    /// <c>odometro_actual</c>, todo en una sola transacción. Devuelve <c>null</c> si el guardado
    /// falló por un conflicto de id duplicado (carrera sobre la llave primaria).
    /// </summary>
    Task<ServicioMantenimiento?> RegistrarAsync(ServicioMantenimiento servicio, Guid vehiculoId, CancellationToken cancellationToken = default);
}
