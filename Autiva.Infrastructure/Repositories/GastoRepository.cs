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

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Gasto.AnyAsync(g => g.Id == id, cancellationToken);

    public Task<CategoriaGasto?> ObtenerCategoriaAsync(byte categoriaId, CancellationToken cancellationToken = default) =>
        _context.CategoriaGasto.FirstOrDefaultAsync(c => c.Id == categoriaId, cancellationToken);

    public async Task<Gasto?> RegistrarAsync(Gasto gasto, CancellationToken cancellationToken = default)
    {
        _context.Gasto.Add(gasto);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Carrera sobre la llave primaria: alguien registró el mismo id
            // entre la validación y el guardado (duplicado al sincronizar).
            return null;
        }

        return gasto;
    }

    public async Task<(IReadOnlyList<Gasto> Items, int TotalRegistros)> ListarHistorialAsync(
        Guid vehiculoId, int pagina, int tamano, int? anio, int? mes, byte? categoriaId,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.Gasto
            .Include(g => g.Categoria)
            .Where(g => g.VehiculoId == vehiculoId && g.EliminadoEn == null);

        if (anio is not null && mes is not null)
        {
            var primerDiaMes = new DateOnly(anio.Value, mes.Value, 1);
            var ultimoDiaMes = primerDiaMes.AddMonths(1).AddDays(-1);
            consulta = consulta.Where(g => g.Fecha >= primerDiaMes && g.Fecha <= ultimoDiaMes);
        }

        if (categoriaId is not null)
            consulta = consulta.Where(g => g.CategoriaId == categoriaId);

        var totalRegistros = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            .OrderByDescending(g => g.Fecha)
            .ThenByDescending(g => g.CreadoEn)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return (items, totalRegistros);
    }
}
