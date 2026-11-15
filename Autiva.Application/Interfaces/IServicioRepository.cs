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

    /// <summary>
    /// Servicio no eliminado por id, de un vehículo no eliminado del usuario; null si no existe,
    /// está eliminado o es de otro usuario (mismo resultado, sin distinguir el motivo).
    /// </summary>
    Task<ServicioMantenimiento?> ObtenerPorUsuarioAsync(Guid usuarioId, Guid servicioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial paginado (más reciente primero) de los servicios no eliminados del vehículo.
    /// Devuelve también el total de registros, para calcular el total de páginas.
    /// </summary>
    Task<(IReadOnlyList<ServicioMantenimiento> Items, int TotalRegistros)> ListarHistorialAsync(
        Guid vehiculoId, int pagina, int tamano, CancellationToken cancellationToken = default);

    /// <summary>
    /// Servicio no eliminado del vehículo con <c>meta_proximo_km</c>, el más reciente por
    /// fecha; null si no tiene ninguno.
    /// </summary>
    Task<ServicioMantenimiento?> ObtenerUltimoConMetaAsync(Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda los cambios del servicio (ya mutado por el servicio de Application) y, si el
    /// nuevo kilometraje es mayor al actual del vehículo, lo actualiza. Todo en una
    /// sola transacción.
    /// </summary>
    Task<ServicioMantenimiento> EditarAsync(ServicioMantenimiento servicio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Borra lógicamente el servicio y sus recordatorios pendientes ligados
    /// (<c>recordatorio.servicio_id</c>), en una sola transacción.
    /// </summary>
    Task EliminarAsync(Guid id, CancellationToken cancellationToken = default);
}
