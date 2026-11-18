using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para recuperar y restablecer la contraseña (HU-027).
/// </summary>
public class RecuperacionContrasenaService : IRecuperacionContrasenaService
{
    private const int DuracionTokenMinutos = 30;
    private const string MensajeRecuperacion = "Si el correo está registrado, te enviamos un enlace para restablecer tu contraseña.";
    private const string MensajeEnlaceInvalido = "El enlace no es válido o ya expiró.";

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenRecuperacionRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEnviadorCorreo _enviadorCorreo;
    private readonly ILogger<RecuperacionContrasenaService> _logger;
    private readonly string _urlBaseFrontend;

    public RecuperacionContrasenaService(
        IUsuarioRepository usuarioRepository,
        ITokenRecuperacionRepository tokenRepository,
        IPasswordHasher passwordHasher,
        IEnviadorCorreo enviadorCorreo,
        IConfiguration configuration,
        ILogger<RecuperacionContrasenaService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _enviadorCorreo = enviadorCorreo;
        _logger = logger;
        _urlBaseFrontend = (configuration["Frontend:UrlBase"] ?? string.Empty).TrimEnd('/');
    }

    public async Task<ResultadoRecuperarContrasena> RecuperarAsync(
        RecuperarContrasenaRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new RecuperarContrasenaRequest();

        var correo = solicitud.Correo?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(correo) || !MailAddress.TryCreate(correo, out _))
            return ResultadoRecuperarContrasena.CrearValidacion("El correo no tiene un formato válido.");

        var usuario = await _usuarioRepository.ObtenerPorCorreoAsync(correo, cancellationToken);

        // Mismo mensaje exista o no la cuenta, para no revelar qué correos están registrados.
        if (usuario is not null)
            await GenerarYEnviarTokenAsync(usuario, cancellationToken);

        return ResultadoRecuperarContrasena.CrearExito(MensajeRecuperacion);
    }

    public async Task<ResultadoRestablecerContrasena> RestablecerAsync(
        RestablecerContrasenaRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new RestablecerContrasenaRequest();

        var tokenPlano = solicitud.Token?.Trim() ?? string.Empty;
        var nuevaContrasena = solicitud.NuevaContrasena ?? string.Empty;

        if (string.IsNullOrWhiteSpace(tokenPlano))
            return ResultadoRestablecerContrasena.CrearValidacion(MensajeEnlaceInvalido);

        var token = await _tokenRepository.ObtenerPorHashAsync(CalcularHash(tokenPlano), cancellationToken);

        // Mismo mensaje si no existe, ya se usó o ya expiró: no se revela cuál es el caso.
        if (token is null || token.UsadoEn is not null || token.ExpiraEn <= DateTime.UtcNow)
            return ResultadoRestablecerContrasena.CrearValidacion(MensajeEnlaceInvalido);

        var erroresContrasena = ValidadorContrasena.Validar(nuevaContrasena);

        if (erroresContrasena.Count > 0)
            return ResultadoRestablecerContrasena.CrearValidacion(erroresContrasena[0]);

        token.Usuario.ContrasenaHash = _passwordHasher.Hash(nuevaContrasena);
        token.Usuario.ContrasenaActualizadaEn = DateTime.UtcNow;

        await _tokenRepository.RestablecerContrasenaAsync(token, token.Usuario, cancellationToken);

        return ResultadoRestablecerContrasena.CrearExito("Tu contraseña se actualizó.");
    }

    private async Task GenerarYEnviarTokenAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        await _tokenRepository.InvalidarPendientesAsync(usuario.Id, cancellationToken);

        var tokenPlano = GenerarTokenAleatorio();
        var token = new TokenRecuperacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = CalcularHash(tokenPlano),
            ExpiraEn = DateTime.UtcNow.AddMinutes(DuracionTokenMinutos)
        };

        await _tokenRepository.AgregarAsync(token, cancellationToken);

        var enlace = $"{_urlBaseFrontend}/restablecer-contrasena?token={tokenPlano}";

        try
        {
            await _enviadorCorreo.EnviarEnlaceRecuperacionAsync(usuario.Correo, enlace, cancellationToken);
        }
        catch (Exception ex)
        {
            // El envío puede fallar (SMTP caído, credenciales inválidas, etc.); la respuesta
            // al cliente sigue siendo la misma 200 para no revelar el estado del correo.
            _logger.LogError(ex, "No se pudo enviar el correo de recuperación de contraseña.");
        }
    }

    private static string GenerarTokenAleatorio()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string CalcularHash(string tokenPlano)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenPlano));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
