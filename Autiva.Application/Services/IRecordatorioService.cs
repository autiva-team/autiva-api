namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Contrato del servicio de recordatorios.
/// </summary>
public interface IRecordatorioService
{
    /// <summary>Obtiene el resumen general de un vehículo del usuario del token.</summary>
    Task<ResultadoResumenVehiculo> ObtenerResumenAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default);

    /// <summary>Crea un recordatorio por fecha para un vehículo del usuario del token.</summary>
    Task<ResultadoRecordatorio> CrearPorFechaAsync(
        Guid vehiculoId,
        CrearRecordatorioFechaRequest solicitud,
        CancellationToken cancellationToken = default);

    /// <summary>Crea un recordatorio por kilometraje para un vehículo del usuario del token.</summary>
    Task<ResultadoRecordatorio> CrearPorKilometrajeAsync(
        Guid vehiculoId,
        CrearRecordatorioKilometrajeRequest solicitud,
        CancellationToken cancellationToken = default);
}
