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

    public Task<bool> ExistePlacasAsync(Guid usuarioId, string placas, CancellationToken cancellationToken = default) =>
        _context.Vehiculo.AnyAsync(
            v => v.UsuarioId == usuarioId &&
                 v.EliminadoEn == null &&
                 v.Placas.ToLower() == placas.ToLower(),
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
                     v.Placas.ToLower() == vehiculo.Placas.ToLower(),
                cancellationToken);

            if (conflicto)
                return false;

            throw;
        }
    }
}
