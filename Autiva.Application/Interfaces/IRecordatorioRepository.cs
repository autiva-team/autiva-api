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
    /// Obtiene el conteo y la lista de recordatorios pendientes del vehículo (con su
    /// vehículo incluido, para calcular el km por día). Quien llama decide cuál es el
    /// próximo según la urgencia (<see cref="OrdenadorRecordatorios"/>).
    /// </summary>
    Task<(int Total, IReadOnlyList<Recordatorio> Pendientes)> ObtenerResumenRecordatoriosAsync(Guid vehiculoId, CancellationToken cancellationToken = default);

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
    /// Recordatorio no eliminado por id, de un vehículo no eliminado del usuario; null si
    /// no existe, está eliminado, su vehículo está eliminado o es de otro usuario (mismo
    /// resultado, sin distinguir el motivo).
    /// </summary>
    Task<Recordatorio?> ObtenerDetalleAsync(Guid usuarioId, Guid recordatorioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un recordatorio existente en la base de datos.
    /// </summary>
    Task ActualizarAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default);
}
