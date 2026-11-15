namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Contrato del servicio de preferencias de la app del usuario autenticado (HU-029).
/// </summary>
public interface IPreferenciasService
{
    Task<ResultadoPreferencias> ObtenerAsync(CancellationToken cancellationToken = default);

    Task<ResultadoPreferencias> ActualizarAsync(
        ActualizarPreferenciasRequest solicitud,
        CancellationToken cancellationToken = default);
}
