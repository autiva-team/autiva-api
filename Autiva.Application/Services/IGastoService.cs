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
}
