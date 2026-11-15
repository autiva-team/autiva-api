using Autiva.Application.Dtos;

namespace Autiva.Application.Services;

/// <summary>
/// Contrato del servicio de gastos de un vehículo.
/// </summary>
public interface IGastoService
{
    /// <summary>
    /// Resumen de gastos del mes (año y mes opcionales; por defecto el mes actual,
    /// hora de México) de un vehículo del usuario del token.
    /// </summary>
    Task<ResultadoResumenGastos> ObtenerResumenAsync(
        Guid vehiculoId,
        int? anio,
        int? mes,
        CancellationToken cancellationToken = default);

    /// <summary>Registra un gasto de otra categoría (lavado, peajes, tenencia, multas, etc.).</summary>
    Task<ResultadoGasto> RegistrarAsync(
        Guid vehiculoId,
        RegistrarGastoRequest solicitud,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial paginado de gastos registrados (tabla <c>gasto</c>), filtrable por
    /// mes (año y mes) y por categoría.
    /// </summary>
    Task<ResultadoHistorialGastos> ListarAsync(
        Guid vehiculoId,
        int? anio,
        int? mes,
        byte? categoriaId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);
}
