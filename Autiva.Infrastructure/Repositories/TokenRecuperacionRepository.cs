using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class TokenRecuperacionRepository : ITokenRecuperacionRepository
{
    private readonly AutivaDbContext _context;

    public TokenRecuperacionRepository(AutivaDbContext context)
    {
        _context = context;
    }

    public Task<TokenRecuperacion?> ObtenerPorHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.TokenRecuperacion
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task InvalidarPendientesAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var pendientes = await _context.TokenRecuperacion
            .Where(t => t.UsuarioId == usuarioId && t.UsadoEn == null)
            .ToListAsync(cancellationToken);

        if (pendientes.Count == 0)
            return;

        var ahora = DateTime.UtcNow;

        foreach (var pendiente in pendientes)
            pendiente.UsadoEn = ahora;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AgregarAsync(TokenRecuperacion token, CancellationToken cancellationToken = default)
    {
        _context.TokenRecuperacion.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RestablecerContrasenaAsync(TokenRecuperacion token, Usuario usuario, CancellationToken cancellationToken = default)
    {
        token.UsadoEn = DateTime.UtcNow;
        usuario.ActualizadoEn = DateTime.UtcNow;

        _context.TokenRecuperacion.Update(token);
        _context.Usuario.Update(usuario);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
