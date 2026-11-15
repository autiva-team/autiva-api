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
    /// Todas las cargas no eliminadas del vehículo, para cálculos de rendimiento y gasto
    /// (dashboard, estadísticas). Sin orden particular; quien llama agrupa como necesite.
    /// </summary>
    Task<IReadOnlyList<CargaCombustible>> ListarNoEliminadasAsync(Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga no eliminada del vehículo, estrictamente anterior (por fecha y, si hay empate,
    /// por odómetro) a <paramref name="fechaHora"/>/<paramref name="odometro"/>; null si es la
    /// primera carga. Se usa para calcular los km recorridos de una carga puntual.
    /// </summary>
    Task<CargaCombustible?> ObtenerAnteriorAsync(Guid vehiculoId, DateTime fechaHora, int odometro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga no eliminada por id, de un vehículo no eliminado del usuario; null si no existe,
    /// está eliminada o es de otro usuario (mismo resultado, sin distinguir el motivo).
    /// </summary>
    Task<CargaCombustible?> ObtenerPorUsuarioAsync(Guid usuarioId, Guid cargaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial paginado (más reciente primero) de las cargas no eliminadas del vehículo,
    /// con filtro opcional de fechas. Devuelve también el total de registros que cumplen el
    /// filtro (antes de paginar), para calcular el total de páginas.
    /// </summary>
    Task<(IReadOnlyList<CargaCombustible> Items, int TotalRegistros)> ListarHistorialAsync(
        Guid vehiculoId, int pagina, int tamano, DateTime? desde, DateTime? hasta, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda la carga y, si su odómetro es mayor al actual del vehículo, actualiza
    /// <c>odometro_actual</c>, todo en una sola transacción. Devuelve <c>null</c> si el guardado
    /// falló por un conflicto de id duplicado (carrera sobre la llave primaria).
    /// </summary>
    Task<CargaCombustible?> RegistrarAsync(CargaCombustible carga, Guid vehiculoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga anterior y siguiente (por fecha y hora) a <paramref name="fechaHora"/>, sin
    /// contar <paramref name="cargaIdExcluida"/> (la que se edita) ni las borradas. Null en
    /// cualquiera de los dos si no hay carga en ese lado.
    /// </summary>
    Task<(CargaCombustible? Anterior, CargaCombustible? Siguiente)> ObtenerVecinasAsync(
        Guid vehiculoId, DateTime fechaHora, Guid cargaIdExcluida, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda los cambios de la carga (ya mutada por el servicio), recalcula el rendimiento
    /// de todas las cargas no borradas del vehículo y, si el nuevo odómetro es mayor al
    /// actual del vehículo, lo actualiza. Todo en una sola transacción.
    /// </summary>
    Task<CargaCombustible> EditarAsync(CargaCombustible carga, CancellationToken cancellationToken = default);

    /// <summary>
    /// Borra lógicamente la carga y recalcula el rendimiento de las demás cargas no
    /// borradas del vehículo, en una sola transacción.
    /// </summary>
    Task EliminarAsync(Guid id, CancellationToken cancellationToken = default);
}
