using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos de vehículos, sin exponer detalles de EF Core.
/// </summary>
public interface IVehiculoRepository
{
    /// <summary>
    /// Indica si el usuario ya tiene un vehículo no eliminado con esas placas.
    /// </summary>
    Task<bool> ExistePlacasAsync(Guid usuarioId, string placas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si el usuario ya tiene al menos un vehículo no eliminado.
    /// </summary>
    Task<bool> TieneVehiculosAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega el vehículo y guarda los cambios. Devuelve <c>false</c> si el guardado
    /// falló por un conflicto de placas duplicadas (carrera sobre el índice único).
    /// </summary>
    Task<bool> AgregarAsync(Vehiculo vehiculo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los vehículos no eliminados del usuario, con el principal primero
    /// y luego del más reciente al más antiguo.
    /// </summary>
    Task<IReadOnlyList<Vehiculo>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el vehículo si existe, no está eliminado y pertenece al usuario;
    /// de lo contrario <c>null</c>, sin distinguir el motivo.
    /// </summary>
    Task<Vehiculo?> ObtenerDetalleAsync(Guid usuarioId, Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desmarca el vehículo activo anterior del usuario (si había uno) y marca
    /// <paramref name="vehiculoId"/> como el nuevo activo, en una sola transacción.
    /// </summary>
    Task<Vehiculo> MarcarActivoAsync(Guid usuarioId, Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza el odómetro actual del vehículo.
    /// </summary>
    Task<Vehiculo> ActualizarOdometroAsync(Guid usuarioId, Guid vehiculoId, int nuevoOdometro, CancellationToken cancellationToken = default);
}
