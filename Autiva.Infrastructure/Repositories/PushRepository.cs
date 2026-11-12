using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class PushRepository : IPushRepository
{
    private readonly AutivaDbContext _context;

    public PushRepository(AutivaDbContext context)
    {
        _context = context;
    }

    public async Task<SuscripcionPush?> ObtenerPorEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await _context.SuscripcionPush
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint, cancellationToken);
    }

    public async Task<IReadOnlyList<SuscripcionPush>> ObtenerActivasPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _context.SuscripcionPush
            .Where(s => s.UsuarioId == usuarioId && s.Activa)
            .ToListAsync(cancellationToken);
    }

    public async Task<SuscripcionPush> GuardarSuscripcionAsync(SuscripcionPush suscripcion, CancellationToken cancellationToken = default)
    {
        var existe = await _context.SuscripcionPush.AnyAsync(s => s.Id == suscripcion.Id, cancellationToken);
        if (existe)
        {
            _context.SuscripcionPush.Update(suscripcion);
        }
        else
        {
            _context.SuscripcionPush.Add(suscripcion);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return suscripcion;
    }

    public async Task DesactivarSuscripcionAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var sub = await _context.SuscripcionPush
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint, cancellationToken);

        if (sub is not null)
        {
            sub.Activa = false;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Notificacion>> ObtenerNotificacionesUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _context.Notificacion
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaEnvio)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notificacion?> ObtenerNotificacionPorIdAsync(Guid notificacionId, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _context.Notificacion
            .FirstOrDefaultAsync(n => n.Id == notificacionId && n.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task GuardarNotificacionAsync(Notificacion notificacion, CancellationToken cancellationToken = default)
    {
        _context.Notificacion.Add(notificacion);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarcarNotificacionLeidaAsync(Guid notificacionId, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var notificacion = await _context.Notificacion
            .FirstOrDefaultAsync(n => n.Id == notificacionId && n.UsuarioId == usuarioId, cancellationToken);

        if (notificacion is not null && !notificacion.Leida)
        {
            notificacion.Leida = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
