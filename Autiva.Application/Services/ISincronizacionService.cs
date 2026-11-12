using Autiva.Application.Dtos;

namespace Autiva.Application.Services;

/// <summary>
/// Contrato del servicio de sincronización offline en lote.
/// </summary>
public interface ISincronizacionService
{
    Task<ResultadoSincronizacion> SincronizarAsync(
        SincronizacionLoteRequest lote,
        CancellationToken cancellationToken = default);
}
