using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class VehiculoRepository : IVehiculoRepository
{
    private readonly AutivaDbContext _context;

    public VehiculoRepository(AutivaDbContext context)
    {
        _context = context;
    }

    // La comparación de placas no distingue mayúsculas porque Azure SQL usa una
    // collation CI (SQL_Latin1_General_CP1_CI_AS). Sin ToLower() la consulta puede
    // usar el índice UX_vehiculo_usuario_placas.
    public Task<bool> ExistePlacasAsync(Guid usuarioId, string placas, CancellationToken cancellationToken = default) =>
        _context.Vehiculo.AnyAsync(
            v => v.UsuarioId == usuarioId &&
                 v.EliminadoEn == null &&
                 v.Placas == placas,
            cancellationToken);

    public Task<bool> TieneVehiculosAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        _context.Vehiculo.AnyAsync(
            v => v.UsuarioId == usuarioId && v.EliminadoEn == null,
            cancellationToken);

    public async Task<bool> AgregarAsync(Vehiculo vehiculo, CancellationToken cancellationToken = default)
    {
        _context.Vehiculo.Add(vehiculo);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Carrera sobre el índice único UX_vehiculo_usuario_placas: alguien
            // registró las mismas placas entre la validación y el guardado.
            var conflicto = await _context.Vehiculo.AnyAsync(
                v => v.UsuarioId == vehiculo.UsuarioId &&
                     v.EliminadoEn == null &&
                     v.Placas == vehiculo.Placas,
                cancellationToken);

            if (conflicto)
                return false;

            throw;
        }
    }

    public async Task<IReadOnlyList<Vehiculo>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _context.Vehiculo
            .Where(v => v.UsuarioId == usuarioId && v.EliminadoEn == null)
            .OrderByDescending(v => v.EsPrincipal)
            .ThenByDescending(v => v.CreadoEn)
            .ToListAsync(cancellationToken);
    }

    // El filtro por UsuarioId va en la misma consulta para que "no existe" y
    // "es de otro usuario" devuelvan lo mismo (null) y no se pueda distinguir.
    public Task<Vehiculo?> ObtenerDetalleAsync(Guid usuarioId, Guid vehiculoId, CancellationToken cancellationToken = default) =>
        _context.Vehiculo.FirstOrDefaultAsync(
            v => v.Id == vehiculoId && v.UsuarioId == usuarioId && v.EliminadoEn == null,
            cancellationToken);

    public async Task<Vehiculo> MarcarActivoAsync(Guid usuarioId, Guid vehiculoId, CancellationToken cancellationToken = default)
    {
        var estrategia = _context.Database.CreateExecutionStrategy();

        // CreateExecutionStrategy envuelve la transacción completa porque el
        // DbContext tiene EnableRetryOnFailure: si hay un fallo transitorio,
        // reintenta desde el BeginTransaction, no solo el SaveChanges.
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);

            // El índice único filtrado UX_vehiculo_principal solo permite un
            // es_principal = 1 por usuario: primero se desmarca el anterior y
            // se guarda, y hasta entonces se marca el nuevo.
            var anterior = await _context.Vehiculo.FirstOrDefaultAsync(
                v => v.UsuarioId == usuarioId && v.EsPrincipal && v.EliminadoEn == null,
                cancellationToken);

            if (anterior is not null)
            {
                anterior.EsPrincipal = false;
                anterior.ActualizadoEn = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var nuevo = await _context.Vehiculo.FirstAsync(
                v => v.Id == vehiculoId && v.UsuarioId == usuarioId,
                cancellationToken);

            nuevo.EsPrincipal = true;
            nuevo.ActualizadoEn = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            await transaccion.CommitAsync(cancellationToken);

            return nuevo;
        });
    }
}