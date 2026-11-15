using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio de las preferencias de la app del usuario autenticado (HU-029).
/// </summary>
public class PreferenciasService : IPreferenciasService
{
    private static readonly string[] UnidadesValidas = { "km_l", "mpg" };

    private readonly IPreferenciasRepository _preferenciasRepository;
    private readonly ICurrentUserService _currentUserService;

    public PreferenciasService(
        IPreferenciasRepository preferenciasRepository,
        ICurrentUserService currentUserService)
    {
        _preferenciasRepository = preferenciasRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoPreferencias> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoPreferencias.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var preferencias = await _preferenciasRepository.ObtenerPorUsuarioAsync(usuarioId.Value, cancellationToken);

        // Sin fila todavía: son los valores por defecto de la tabla, no un error.
        var respuesta = preferencias is null ? ValoresPorDefecto() : MapearRespuesta(preferencias);

        return ResultadoPreferencias.CrearExito(respuesta);
    }

    public async Task<ResultadoPreferencias> ActualizarAsync(
        ActualizarPreferenciasRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new ActualizarPreferenciasRequest();

        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoPreferencias.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var unidadMedida = solicitud.UnidadMedida?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!UnidadesValidas.Contains(unidadMedida))
            return ResultadoPreferencias.CrearValidacion("La unidad de medida debe ser \"km_l\" o \"mpg\".");

        var existente = await _preferenciasRepository.ObtenerPorUsuarioAsync(usuarioId.Value, cancellationToken);

        var guardado = existente is null
            ? await _preferenciasRepository.AgregarAsync(
                NuevaPreferencia(usuarioId.Value, solicitud, unidadMedida), cancellationToken)
            : await _preferenciasRepository.ActualizarAsync(
                AplicarCambios(existente, solicitud, unidadMedida), cancellationToken);

        return ResultadoPreferencias.CrearExito(MapearRespuesta(guardado));
    }

    private static PreferenciasUsuario NuevaPreferencia(
        Guid usuarioId, ActualizarPreferenciasRequest solicitud, string unidadMedida) => new()
    {
        UsuarioId = usuarioId,
        NotificacionesPush = solicitud.NotificacionesPush,
        UnidadMedida = unidadMedida,
        RespaldoNube = solicitud.RespaldoNube,
        ModoOffline = solicitud.ModoOffline
    };

    private static PreferenciasUsuario AplicarCambios(
        PreferenciasUsuario existente, ActualizarPreferenciasRequest solicitud, string unidadMedida)
    {
        existente.NotificacionesPush = solicitud.NotificacionesPush;
        existente.UnidadMedida = unidadMedida;
        existente.RespaldoNube = solicitud.RespaldoNube;
        existente.ModoOffline = solicitud.ModoOffline;

        return existente;
    }

    private static PreferenciasResponse ValoresPorDefecto() => new()
    {
        NotificacionesPush = true,
        UnidadMedida = "km_l",
        RespaldoNube = true,
        ModoOffline = true,
        UltimaSincronizacion = null
    };

    private static PreferenciasResponse MapearRespuesta(PreferenciasUsuario p) => new()
    {
        NotificacionesPush = p.NotificacionesPush,
        UnidadMedida = p.UnidadMedida,
        RespaldoNube = p.RespaldoNube,
        ModoOffline = p.ModoOffline,
        UltimaSincronizacion = p.UltimaSincronizacion
    };
}
