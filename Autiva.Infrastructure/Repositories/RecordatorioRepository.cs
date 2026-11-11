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

        // El próximo recordatorio por fecha es el con FechaLimite más cercana;
        // los de tipo kilometraje (FechaLimite null) quedan al final.
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
}
