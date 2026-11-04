namespace Autiva.Application.Services;

/// <summary>
/// Contrato del servicio de rendimiento de combustible, compartido por cargas,
/// dashboard y estadísticas.
/// </summary>
public interface IRendimientoService
{
    /// <summary>Rendimiento y gasto de un vehículo del usuario del token.</summary>
    Task<ResultadoRendimiento> ObtenerAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default);
}
