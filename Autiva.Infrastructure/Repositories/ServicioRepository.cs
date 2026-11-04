using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class ServicioRepository : IServicioRepository
{
    private readonly AutivaDbContext _context;

    public ServicioRepository(AutivaDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ServicioMantenimiento.AnyAsync(s => s.Id == id, cancellationToken);

    public Task<TipoServicio?> ObtenerTipoServicioAsync(byte tipoServicioId, CancellationToken cancellationToken = default) =>
        _context.TipoServicio.FirstOrDefaultAsync(t => t.Id == tipoServicioId, cancellationToken);

    public async Task<ServicioMantenimiento?> RegistrarAsync(ServicioMantenimiento servicio, Guid vehiculoId, CancellationToken cancellationToken = default)
    {
        var estrategia = _context.Database.CreateExecutionStrategy();

        // CreateExecutionStrategy envuelve la transacción completa porque el DbContext
        // tiene EnableRetryOnFailure: si hay un fallo transitorio, reintenta desde el
        // BeginTransaction, no solo el SaveChanges (igual que al registrar una carga).
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);

            _context.ServicioMantenimiento.Add(servicio);

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

            if (servicio.KilometrajeServicio > vehiculo.OdometroActual)
            {
                vehiculo.OdometroActual = servicio.KilometrajeServicio;
                vehiculo.ActualizadoEn = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaccion.CommitAsync(cancellationToken);

            return servicio;
        });
    }

    public Task<ServicioMantenimiento?> ObtenerPorUsuarioAsync(Guid usuarioId, Guid servicioId, CancellationToken cancellationToken = default) =>
        _context.ServicioMantenimiento
            .Include(s => s.TipoServicio)
            .FirstOrDefaultAsync(
                s => s.Id == servicioId && s.EliminadoEn == null &&
                     s.Vehiculo.UsuarioId == usuarioId && s.Vehiculo.EliminadoEn == null,
                cancellationToken);

    public async Task<(IReadOnlyList<ServicioMantenimiento> Items, int TotalRegistros)> ListarHistorialAsync(
        Guid vehiculoId, int pagina, int tamano, CancellationToken cancellationToken = default)
    {
        var consulta = _context.ServicioMantenimiento
            .Include(s => s.TipoServicio)
            .Where(s => s.VehiculoId == vehiculoId && s.EliminadoEn == null);

        var totalRegistros = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            .OrderByDescending(s => s.Fecha)
            .ThenByDescending(s => s.KilometrajeServicio)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return (items, totalRegistros);
    }
}
