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

    /// <summary>Lista los recordatorios de un vehículo específico con información de vencimiento.</summary>
    Task<ResultadoListaRecordatorios> ListarPorVehiculoAsync(
        Guid vehiculoId,
        string? estado = null,
        bool soloVencidos = false,
        CancellationToken cancellationToken = default);

    /// <summary>Lista los recordatorios de todos los vehículos del usuario autenticado.</summary>
    Task<ResultadoListaRecordatorios> ListarPorUsuarioAsync(
        string? estado = "pendiente",
        bool soloVencidos = false,
        CancellationToken cancellationToken = default);

    /// <summary>Marca un recordatorio como concluido.</summary>
    Task<ResultadoRecordatorio> ConcluirAsync(
        Guid recordatorioId,
        CancellationToken cancellationToken = default);
}
