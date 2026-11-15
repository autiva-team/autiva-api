using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class GastoRepository : IGastoRepository
{
    private readonly AutivaDbContext _context;

    public GastoRepository(AutivaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<VwGastosConsolidados>> ListarPorVehiculoYRangoAsync(
        Guid vehiculoId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        return await _context.VwGastosConsolidados
            .Where(g => g.VehiculoId == vehiculoId && g.Fecha >= desde && g.Fecha <= hasta)
            .ToListAsync(cancellationToken);
    }
}
