using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos para la sincronización idempotente de registros offline.
/// </summary>
public interface ISincronizacionRepository
{
    Task<bool> ExisteCargaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExisteServicioAsync(Guid id, CancellationToken cancellationToken = default);
    Task GuardarCargaAsync(CargaCombustible carga, CancellationToken cancellationToken = default);
    Task GuardarServicioAsync(ServicioMantenimiento servicio, CancellationToken cancellationToken = default);
}
