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
}
