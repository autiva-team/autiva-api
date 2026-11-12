using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class RecordatorioRepository : IRecordatorioRepository
{
    private readonly AutivaDbContext _context;

    public RecordatorioRepository(AutivaDbContext context)
    {
        _context = context;
    }

    public async Task<(int Total, DateTime? UltimaCarga)> ObtenerResumenCargasAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.CargaCombustible
            .Where(c => c.VehiculoId == vehiculoId && c.EliminadoEn == null);

        var total = await consulta.CountAsync(cancellationToken);
        var ultimaCarga = await consulta
            .OrderByDescending(c => c.FechaHora)
            .Select(c => (DateTime?)c.FechaHora)
            .FirstOrDefaultAsync(cancellationToken);

        return (total, ultimaCarga);
    }

    public async Task<(int Total, DateOnly? UltimoServicio)> ObtenerResumenServiciosAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.ServicioMantenimiento
            .Where(s => s.VehiculoId == vehiculoId && s.EliminadoEn == null);

        var total = await consulta.CountAsync(cancellationToken);
        var ultimoServicio = await consulta
            .OrderByDescending(s => s.Fecha)
            .Select(s => (DateOnly?)s.Fecha)
            .FirstOrDefaultAsync(cancellationToken);

        return (total, ultimoServicio);
    }

    public async Task<(int Total, Recordatorio? Proximo)> ObtenerResumenRecordatoriosAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.Recordatorio
            .Where(r => r.VehiculoId == vehiculoId &&
                        r.EliminadoEn == null &&
                        r.Estado == "pendiente");

        var total = await consulta.CountAsync(cancellationToken);

        var proximo = await consulta
            .OrderBy(r => r.FechaLimite == null ? 1 : 0)
            .ThenBy(r => r.FechaLimite)
            .ThenBy(r => r.KmObjetivo)
            .FirstOrDefaultAsync(cancellationToken);

        return (total, proximo);
    }

    public async Task<Recordatorio> AgregarAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default)
    {
        _context.Recordatorio.Add(recordatorio);
        await _context.SaveChangesAsync(cancellationToken);
        return recordatorio;
    }

    public async Task<IReadOnlyList<Recordatorio>> ObtenerPorVehiculoAsync(
        Guid vehiculoId,
        string? estado = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.Recordatorio
            .Include(r => r.Vehiculo)
            .Where(r => r.VehiculoId == vehiculoId && r.EliminadoEn == null);

        if (!string.IsNullOrWhiteSpace(estado))
        {
            consulta = consulta.Where(r => r.Estado == estado.Trim().ToLower());
        }

        return await consulta
            .OrderBy(r => r.FechaLimite == null ? 1 : 0)
            .ThenBy(r => r.FechaLimite)
            .ThenBy(r => r.KmObjetivo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Recordatorio>> ObtenerPorUsuarioAsync(
        Guid usuarioId,
        string? estado = "pendiente",
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.Recordatorio
            .Include(r => r.Vehiculo)
            .Where(r => r.Vehiculo.UsuarioId == usuarioId &&
                        r.Vehiculo.EliminadoEn == null &&
                        r.EliminadoEn == null);

        if (!string.IsNullOrWhiteSpace(estado))
        {
            consulta = consulta.Where(r => r.Estado == estado.Trim().ToLower());
        }

        return await consulta
            .OrderBy(r => r.FechaLimite == null ? 1 : 0)
            .ThenBy(r => r.FechaLimite)
            .ThenBy(r => r.KmObjetivo)
            .ToListAsync(cancellationToken);
    }

    public async Task<Recordatorio?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Recordatorio
            .Include(r => r.Vehiculo)
            .FirstOrDefaultAsync(r => r.Id == id && r.EliminadoEn == null, cancellationToken);
    }

    public async Task ActualizarAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default)
    {
        recordatorio.ActualizadoEn = DateTime.UtcNow;
        _context.Recordatorio.Update(recordatorio);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
