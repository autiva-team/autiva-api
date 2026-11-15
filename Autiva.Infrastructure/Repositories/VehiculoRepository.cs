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

    public async Task<Vehiculo> ActualizarOdometroAsync(Guid usuarioId, Guid vehiculoId, int nuevoOdometro, CancellationToken cancellationToken = default)
    {
        var vehiculo = await _context.Vehiculo.FirstAsync(
            v => v.Id == vehiculoId && v.UsuarioId == usuarioId,
            cancellationToken);

        vehiculo.OdometroActual = nuevoOdometro;
        vehiculo.ActualizadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return vehiculo;
    }

    public Task<bool> ExistePlacasEnOtroVehiculoAsync(Guid usuarioId, string placas, Guid vehiculoIdExcluido, CancellationToken cancellationToken = default) =>
        _context.Vehiculo.AnyAsync(
            v => v.UsuarioId == usuarioId &&
                 v.EliminadoEn == null &&
                 v.Placas == placas &&
                 v.Id != vehiculoIdExcluido,
            cancellationToken);

    public async Task<Vehiculo> EditarAsync(
        Guid usuarioId, Guid vehiculoId, string marca, string modelo, short anio, string placas,
        string? color, string? fotoUrl, string estatus, CancellationToken cancellationToken = default)
    {
        var vehiculo = await _context.Vehiculo.FirstAsync(
            v => v.Id == vehiculoId && v.UsuarioId == usuarioId,
            cancellationToken);

        vehiculo.Marca = marca;
        vehiculo.Modelo = modelo;
        vehiculo.Anio = anio;
        vehiculo.Placas = placas;
        vehiculo.Color = color;
        vehiculo.FotoUrl = fotoUrl;
        vehiculo.Estatus = estatus;
        vehiculo.ActualizadoEn = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return vehiculo;
    }

    public Task<Vehiculo?> ObtenerMasRecienteDistintoAsync(Guid usuarioId, Guid vehiculoIdExcluido, CancellationToken cancellationToken = default) =>
        _context.Vehiculo
            .Where(v => v.UsuarioId == usuarioId && v.Id != vehiculoIdExcluido && v.EliminadoEn == null)
            .OrderByDescending(v => v.CreadoEn)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task EliminarAsync(Guid usuarioId, Guid vehiculoId, Guid? nuevoPrincipalId, CancellationToken cancellationToken = default)
    {
        var estrategia = _context.Database.CreateExecutionStrategy();

        // Misma estrategia que en MarcarActivoAsync/AgregarAsync: CreateExecutionStrategy
        // envuelve la transacción completa porque el DbContext tiene EnableRetryOnFailure.
        await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);

            var ahora = DateTime.UtcNow;

            await MarcarEliminadoEnRelacionadosAsync(vehiculoId, ahora, cancellationToken);

            var vehiculo = await _context.Vehiculo.FirstAsync(
                v => v.Id == vehiculoId && v.UsuarioId == usuarioId, cancellationToken);

            vehiculo.EliminadoEn = ahora;
            vehiculo.EsPrincipal = false;
            vehiculo.ActualizadoEn = ahora;
            await _context.SaveChangesAsync(cancellationToken);

            if (nuevoPrincipalId is not null)
            {
                // El índice único filtrado UX_vehiculo_principal exige que el vehículo
                // borrado ya haya quedado desmarcado (arriba) antes de marcar al nuevo.
                var nuevoPrincipal = await _context.Vehiculo.FirstAsync(
                    v => v.Id == nuevoPrincipalId.Value && v.UsuarioId == usuarioId, cancellationToken);

                nuevoPrincipal.EsPrincipal = true;
                nuevoPrincipal.ActualizadoEn = ahora;
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaccion.CommitAsync(cancellationToken);
        });
    }

    /// <summary>
    /// Pone <c>eliminado_en</c> a todo lo del vehículo que aún no esté borrado: cargas,
    /// servicios, gastos, recordatorios, pólizas, verificaciones y documentos.
    /// </summary>
    private async Task MarcarEliminadoEnRelacionadosAsync(Guid vehiculoId, DateTime ahora, CancellationToken cancellationToken)
    {
        var cargas = await _context.CargaCombustible
            .Where(c => c.VehiculoId == vehiculoId && c.EliminadoEn == null)
            .ToListAsync(cancellationToken);
        foreach (var carga in cargas)
        {
            carga.EliminadoEn = ahora;
            carga.ActualizadoEn = ahora;
        }

        var servicios = await _context.ServicioMantenimiento
            .Where(s => s.VehiculoId == vehiculoId && s.EliminadoEn == null)
            .ToListAsync(cancellationToken);
        foreach (var servicio in servicios)
        {
            servicio.EliminadoEn = ahora;
            servicio.ActualizadoEn = ahora;
        }

        var gastos = await _context.Gasto
            .Where(g => g.VehiculoId == vehiculoId && g.EliminadoEn == null)
            .ToListAsync(cancellationToken);
        foreach (var gasto in gastos)
        {
            gasto.EliminadoEn = ahora;
            gasto.ActualizadoEn = ahora;
        }

        var recordatorios = await _context.Recordatorio
            .Where(r => r.VehiculoId == vehiculoId && r.EliminadoEn == null)
            .ToListAsync(cancellationToken);
        foreach (var recordatorio in recordatorios)
        {
            recordatorio.EliminadoEn = ahora;
            recordatorio.ActualizadoEn = ahora;
        }

        var polizas = await _context.PolizaSeguro
            .Where(p => p.VehiculoId == vehiculoId && p.EliminadoEn == null)
            .ToListAsync(cancellationToken);
        foreach (var poliza in polizas)
        {
            poliza.EliminadoEn = ahora;
            poliza.ActualizadoEn = ahora;
        }

        var verificaciones = await _context.VerificacionVehicular
            .Where(v => v.VehiculoId == vehiculoId && v.EliminadoEn == null)
            .ToListAsync(cancellationToken);
        foreach (var verificacion in verificaciones)
        {
            verificacion.EliminadoEn = ahora;
            verificacion.ActualizadoEn = ahora;
        }

        var documentos = await _context.DocumentoVehicular
            .Where(d => d.VehiculoId == vehiculoId && d.EliminadoEn == null)
            .ToListAsync(cancellationToken);
        foreach (var documento in documentos)
        {
            documento.EliminadoEn = ahora;
            documento.ActualizadoEn = ahora;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}