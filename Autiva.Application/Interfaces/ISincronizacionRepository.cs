using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos para la sincronización idempotente de registros offline.
/// </summary>
public interface ISincronizacionRepository
{
    Task<bool> ExisteCargaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExisteServicioAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Guarda la carga; devuelve <c>false</c> (sin lanzar) si la base la rechaza, por
    /// ejemplo por una restricción CHECK, para no detener el resto del lote de sincronización.</summary>
    Task<bool> GuardarCargaAsync(CargaCombustible carga, CancellationToken cancellationToken = default);

    /// <summary>Igual que <see cref="GuardarCargaAsync"/>, para un servicio de mantenimiento.</summary>
    Task<bool> GuardarServicioAsync(ServicioMantenimiento servicio, CancellationToken cancellationToken = default);
}
