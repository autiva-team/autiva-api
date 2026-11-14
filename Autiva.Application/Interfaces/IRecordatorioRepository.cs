using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos de recordatorios.
/// </summary>
public interface IRecordatorioRepository
{
    /// <summary>
    /// Obtiene el conteo y la fecha de la última carga de combustible de un vehículo.
    /// </summary>
    Task<(int Total, DateTime? UltimaCarga)> ObtenerResumenCargasAsync(Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el conteo y la fecha del último servicio de mantenimiento de un vehículo.
    /// </summary>
    Task<(int Total, DateOnly? UltimoServicio)> ObtenerResumenServiciosAsync(Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el conteo de recordatorios pendientes y el próximo (por fecha límite).
    /// </summary>
    Task<(int Total, Recordatorio? Proximo)> ObtenerResumenRecordatoriosAsync(Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un nuevo recordatorio y retorna la entidad guardada.
    /// </summary>
    Task<Recordatorio> AgregarAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los recordatorios de un vehículo específico, opcionalmente filtrando por estado.
    /// </summary>
    Task<IReadOnlyList<Recordatorio>> ObtenerPorVehiculoAsync(Guid vehiculoId, string? estado = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los recordatorios de todos los vehículos de un usuario autenticado.
    /// </summary>
    Task<IReadOnlyList<Recordatorio>> ObtenerPorUsuarioAsync(Guid usuarioId, string? estado = "pendiente", CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un recordatorio por su identificador único junto con su vehículo.
    /// </summary>
    Task<Recordatorio?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un recordatorio existente en la base de datos.
    /// </summary>
    Task ActualizarAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default);
}
