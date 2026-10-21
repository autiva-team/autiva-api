using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Autiva.Api.Data;
using Autiva.Api.Dtos;
using Autiva.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Autiva.Api.Services;

/// <summary>
/// Lógica de negocio para el registro e inicio de sesión de cuentas.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AutivaDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AutivaDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<ResultadoRegistro> RegistrarAsync(
        RegistroUsuarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        solicitud ??= new RegistroUsuarioRequest();

        var nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        var correo = solicitud.Correo?.Trim().ToLowerInvariant() ?? string.Empty;
        var contrasena = solicitud.Contrasena ?? string.Empty;

        var existe = await _context.Usuario
            .AnyAsync(u => u.Correo.ToLower() == correo, cancellationToken);

        if (existe)
            return ResultadoRegistro.CrearConflicto("Ya existe una cuenta con ese correo.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Correo = correo,
            ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(contrasena),
            TipoCuenta = "estandar",
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        _context.Usuario.Add(usuario);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Carrera sobre el índice único UQ_usuario_correo: alguien registró
            // el mismo correo entre la validación anterior y el guardado.
            var conflicto = await _context.Usuario
                .AnyAsync(u => u.Correo.ToLower() == correo, cancellationToken);

            if (conflicto)
                return ResultadoRegistro.CrearConflicto("Ya existe una cuenta con ese correo.");

            throw;
        }

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

        var usuario = await _context.Usuario
            .FirstOrDefaultAsync(u => u.Correo.ToLower() == correo, cancellationToken);

        // Mensaje genérico para no revelar si el correo existe.
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(contrasena, usuario.ContrasenaHash))
            return ResultadoLogin.Fallo("Correo o contraseña incorrectos.");

        var token = GenerarToken(usuario);
        var expiraEn = DateTime.UtcNow.AddMinutes(ObtenerMinutosExpiracion());

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

    private int ObtenerMinutosExpiracion()
    {
        var minutos = _configuration.GetValue<int?>("Jwt:ExpiresMinutes");
        return minutos is > 0 ? minutos.Value : 60;
    }

    private string GenerarToken(Usuario usuario)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key no configurado.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Role, usuario.TipoCuenta),
            new Claim("tipo_cuenta", usuario.TipoCuenta),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(issuer) ? null : issuer,
            audience: string.IsNullOrWhiteSpace(audience) ? null : audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(ObtenerMinutosExpiracion()),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
