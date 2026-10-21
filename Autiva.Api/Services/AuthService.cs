using System.Net.Mail;
using Autiva.Api.Data;
using Autiva.Api.Dtos;
using Autiva.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Api.Services;

/// <summary>
/// Lógica de negocio para el registro de cuentas.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AutivaDbContext _context;

    public AuthService(AutivaDbContext context)
    {
        _context = context;
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
}
