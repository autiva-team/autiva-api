using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para el registro e inicio de sesión de cuentas.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtGenerator _jwtGenerator;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtGenerator jwtGenerator)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<ResultadoRegistro> RegistrarAsync(
        RegistroUsuarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        solicitud ??= new RegistroUsuarioRequest();

        var nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        var correo = solicitud.Correo?.Trim().ToLowerInvariant() ?? string.Empty;
        var contrasena = solicitud.Contrasena ?? string.Empty;

        var existe = await _usuarioRepository.ExisteCorreoAsync(correo, cancellationToken);

        if (existe)
            return ResultadoRegistro.CrearConflicto("Ya existe una cuenta con ese correo.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Correo = correo,
            ContrasenaHash = _passwordHasher.Hash(contrasena),
            TipoCuenta = "estandar",
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var agregado = await _usuarioRepository.AgregarAsync(usuario, cancellationToken);

        if (!agregado)
            return ResultadoRegistro.CrearConflicto("Ya existe una cuenta con ese correo.");

        var respuesta = new RegistroUsuarioResponse
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            TipoCuenta = usuario.TipoCuenta,
            CreadoEn = usuario.CreadoEn
        };

        return ResultadoRegistro.CrearExito(respuesta);
    }

    public async Task<ResultadoLogin> LoginAsync(
        LoginRequest solicitud,
        CancellationToken cancellationToken)
    {
        solicitud ??= new LoginRequest();

        var correo = solicitud.Correo?.Trim().ToLowerInvariant() ?? string.Empty;
        var contrasena = solicitud.Contrasena ?? string.Empty;

        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            return ResultadoLogin.Fallo("Correo y contraseña son obligatorios.");

        var usuario = await _usuarioRepository.ObtenerPorCorreoAsync(correo, cancellationToken);

        // Mensaje genérico para no revelar si el correo existe.
        if (usuario is null || !_passwordHasher.Verify(contrasena, usuario.ContrasenaHash))
            return ResultadoLogin.Fallo("Correo o contraseña incorrectos.");

        var (token, expiraEn) = _jwtGenerator.GenerarToken(usuario);

        var datos = new LoginResponse
        {
            Token = token,
            ExpiraEn = expiraEn,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            TipoCuenta = usuario.TipoCuenta
        };

        return ResultadoLogin.CrearExito(datos);
    }
}
