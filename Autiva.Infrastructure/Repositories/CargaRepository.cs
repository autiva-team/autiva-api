using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class CargaRepository : ICargaRepository
{
    private readonly AutivaDbContext _context;

    public CargaRepository(AutivaDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.CargaCombustible.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<TipoCombustible?> ObtenerTipoCombustibleAsync(byte tipoCombustibleId, CancellationToken cancellationToken = default) =>
        _context.TipoCombustible.FirstOrDefaultAsync(t => t.Id == tipoCombustibleId, cancellationToken);

    public Task<CargaCombustible?> ObtenerUltimaAsync(Guid vehiculoId, CancellationToken cancellationToken = default) =>
        _context.CargaCombustible
            .Where(c => c.VehiculoId == vehiculoId && c.EliminadoEn == null)
            .OrderByDescending(c => c.FechaHora)
            .ThenByDescending(c => c.Odometro)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<CargaCombustible?> ObtenerUltimaTanqueLlenoAsync(Guid vehiculoId, CancellationToken cancellationToken = default) =>
        _context.CargaCombustible
            .Where(c => c.VehiculoId == vehiculoId && c.TanqueLleno && c.EliminadoEn == null)
            .OrderByDescending(c => c.FechaHora)
            .ThenByDescending(c => c.Odometro)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<decimal> SumarLitrosPosterioresAsync(Guid vehiculoId, DateTime fechaHoraDesde, CancellationToken cancellationToken = default) =>
        _context.CargaCombustible
            .Where(c => c.VehiculoId == vehiculoId && c.EliminadoEn == null && c.FechaHora > fechaHoraDesde)
            .SumAsync(c => c.Litros, cancellationToken);

    public async Task<CargaCombustible?> RegistrarAsync(CargaCombustible carga, Guid vehiculoId, CancellationToken cancellationToken = default)
    {
        var estrategia = _context.Database.CreateExecutionStrategy();

        // CreateExecutionStrategy envuelve la transacción completa porque el DbContext
        // tiene EnableRetryOnFailure: si hay un fallo transitorio, reintenta desde el
        // BeginTransaction, no solo el SaveChanges (igual que en MarcarActivoAsync).
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);

            _context.CargaCombustible.Add(carga);

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

            var vehiculo = await _context.Vehiculo.FirstAsync(v => v.Id == vehiculoId, cancellationToken);

            if (carga.Odometro > vehiculo.OdometroActual)
            {
                vehiculo.OdometroActual = carga.Odometro;
                vehiculo.ActualizadoEn = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaccion.CommitAsync(cancellationToken);

            return carga;
        });
    }
}
