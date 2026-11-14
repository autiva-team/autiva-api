using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class SincronizacionRepository : ISincronizacionRepository
{
    private readonly AutivaDbContext _context;

    public SincronizacionRepository(AutivaDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExisteCargaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CargaCombustible.AnyAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExisteServicioAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ServicioMantenimiento.AnyAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> GuardarCargaAsync(CargaCombustible carga, CancellationToken cancellationToken = default)
    {
        _context.CargaCombustible.Add(carga);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Se desvincula del change tracker para que el siguiente SaveChanges del lote
            // no vuelva a intentar guardar esta misma carga rechazada.
            _context.Entry(carga).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> GuardarServicioAsync(ServicioMantenimiento servicio, CancellationToken cancellationToken = default)
    {
        _context.ServicioMantenimiento.Add(servicio);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(servicio).State = EntityState.Detached;
            return false;
        }
    }
}
