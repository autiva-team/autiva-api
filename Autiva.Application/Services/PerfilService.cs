using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio del perfil del usuario autenticado (HU-030).
/// </summary>
public class PerfilService : IPerfilService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public PerfilService(
        IUsuarioRepository usuarioRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _usuarioRepository = usuarioRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoPerfil> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoPerfil.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId.Value, cancellationToken);

        if (usuario is null)
            return ResultadoPerfil.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var numeroVehiculos = await ContarVehiculosAsync(usuarioId.Value, cancellationToken);

        return ResultadoPerfil.CrearExito(MapearRespuesta(usuario, numeroVehiculos));
    }

    public async Task<ResultadoPerfil> EditarAsync(
        EditarPerfilRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new EditarPerfilRequest();

        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoPerfil.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId.Value, cancellationToken);

        if (usuario is null)
            return ResultadoPerfil.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        var correo = solicitud.Correo?.Trim().ToLowerInvariant() ?? string.Empty;

        var errores = ValidadorPerfil.Validar(nombre, correo);

        if (errores.Count > 0)
            return ResultadoPerfil.CrearValidacion(errores);

        if (correo != usuario.Correo)
        {
            var enUso = await _usuarioRepository.ExisteCorreoEnOtroUsuarioAsync(correo, usuarioId.Value, cancellationToken);

            if (enUso)
                return ResultadoPerfil.CrearConflicto("Ese correo ya está en uso.");
        }

        var actualizado = await _usuarioRepository.EditarPerfilAsync(usuarioId.Value, nombre, correo, cancellationToken);
        var numeroVehiculos = await ContarVehiculosAsync(usuarioId.Value, cancellationToken);

        return ResultadoPerfil.CrearExito(MapearRespuesta(actualizado, numeroVehiculos));
    }

    private async Task<int> ContarVehiculosAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var vehiculos = await _vehiculoRepository.ListarAsync(usuarioId, cancellationToken);
        return vehiculos.Count;
    }

    private static PerfilResponse MapearRespuesta(Usuario usuario, int numeroVehiculos) => new()
    {
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        Correo = usuario.Correo,
        TipoCuenta = usuario.TipoCuenta,
        NumeroVehiculos = numeroVehiculos,
        CreadoEn = usuario.CreadoEn
    };
}
