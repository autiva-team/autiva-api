using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class PreferenciasRepository : IPreferenciasRepository
{
    private readonly AutivaDbContext _context;

    public PreferenciasRepository(AutivaDbContext context)
    {
        _context = context;
    }

    public Task<PreferenciasUsuario?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        _context.PreferenciasUsuario.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, cancellationToken);

    public async Task<PreferenciasUsuario> AgregarAsync(PreferenciasUsuario preferencias, CancellationToken cancellationToken = default)
    {
        _context.PreferenciasUsuario.Add(preferencias);
        await _context.SaveChangesAsync(cancellationToken);
        return preferencias;
    }

    public async Task<PreferenciasUsuario> ActualizarAsync(PreferenciasUsuario preferencias, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return preferencias;
    }
}
