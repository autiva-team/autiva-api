using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Acceso a datos de cargas de combustible, sin exponer detalles de EF Core.
/// </summary>
public interface ICargaRepository
{
    /// <summary>Indica si ya existe una carga con ese id (para evitar duplicados al sincronizar).</summary>
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el tipo de combustible del catálogo, o null si no existe.</summary>
    Task<TipoCombustible?> ObtenerTipoCombustibleAsync(byte tipoCombustibleId, CancellationToken cancellationToken = default);

    /// <summary>Carga no eliminada más reciente del vehículo (de cualquier tipo), o null si no tiene ninguna.</summary>
    Task<CargaCombustible?> ObtenerUltimaAsync(Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>Carga de tanque lleno no eliminada más reciente del vehículo, o null si no tiene ninguna.</summary>
    Task<CargaCombustible?> ObtenerUltimaTanqueLlenoAsync(Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>Suma de litros de las cargas no eliminadas del vehículo posteriores a <paramref name="fechaHoraDesde"/>.</summary>
    Task<decimal> SumarLitrosPosterioresAsync(Guid vehiculoId, DateTime fechaHoraDesde, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda la carga y, si su odómetro es mayor al actual del vehículo, actualiza
    /// <c>odometro_actual</c>, todo en una sola transacción. Devuelve <c>null</c> si el guardado
    /// falló por un conflicto de id duplicado (carrera sobre la llave primaria).
    /// </summary>
    Task<CargaCombustible?> RegistrarAsync(CargaCombustible carga, Guid vehiculoId, CancellationToken cancellationToken = default);
}
