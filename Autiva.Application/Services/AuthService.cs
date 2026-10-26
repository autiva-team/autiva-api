using System.Net.Mail;
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

        var errores = ValidarRegistro(nombre, correo, contrasena);

        if (errores.Count > 0)
            return ResultadoRegistro.CrearValidacion(errores);

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

    private static List<string> ValidarRegistro(string nombre, string correo, string contrasena)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(nombre))
            errores.Add("El nombre es obligatorio.");
        else if (nombre.Length > 120)
            errores.Add("El nombre no puede superar los 120 caracteres.");

        if (string.IsNullOrWhiteSpace(correo))
            errores.Add("El correo es obligatorio.");
        else if (correo.Length > 150)
            errores.Add("El correo no puede superar los 150 caracteres.");
        else if (!MailAddress.TryCreate(correo, out _))
            errores.Add("El correo no tiene un formato válido.");

        if (string.IsNullOrWhiteSpace(contrasena))
            errores.Add("La contraseña es obligatoria.");
        else if (contrasena.Length < 8)
            errores.Add("La contraseña debe tener al menos 8 caracteres.");
        else if (contrasena.Length > 72)
            errores.Add("La contraseña no puede superar los 72 caracteres (límite de BCrypt).");

        return errores;
    }
}
