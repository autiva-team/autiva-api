using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para sincronizar en lote cargas y servicios capturados offline.
/// Aplica idempotencia por ID para evitar duplicados.
/// </summary>
public class SincronizacionService : ISincronizacionService
{
    private readonly ISincronizacionRepository _sincronizacionRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public SincronizacionService(
        ISincronizacionRepository sincronizacionRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _sincronizacionRepository = sincronizacionRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoSincronizacion> SincronizarAsync(
        SincronizacionLoteRequest lote,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoSincronizacion.CrearNoAutorizado("Token inválido o usuario no identificado.");

        lote ??= new SincronizacionLoteRequest();

        var respuesta = new SincronizacionLoteResponse
        {
            CargasRecibidas = lote.Cargas.Count,
            ServiciosRecibidos = lote.Servicios.Count
        };

        // Cache local de vehículos validados para no repetir consultas innecesarias
        var vehiculosUsuario = new Dictionary<Guid, Vehiculo?>();

        async Task<Vehiculo?> ObtenerVehiculoValidoAsync(Guid vehiculoId)
        {
            if (vehiculosUsuario.TryGetValue(vehiculoId, out var v))
                return v;

            var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);
            vehiculosUsuario[vehiculoId] = vehiculo;
            return vehiculo;
        }

        // 1. Procesar cargas offline
        foreach (var c in lote.Cargas)
        {
            if (c.Id == Guid.Empty)
                continue;

            var vehiculo = await ObtenerVehiculoValidoAsync(c.VehiculoId);
            if (vehiculo is null)
                continue; // El vehículo no le pertenece al usuario

            // Comprobar idempotencia: si ya existe el ID, se ignora la inserción sin error
            if (await _sincronizacionRepository.ExisteCargaAsync(c.Id, cancellationToken))
            {
                respuesta.CargasIgnoradasDuplicadas++;
                respuesta.CargasSincronizadasIds.Add(c.Id);
                continue;
            }

            var nuevaCarga = new CargaCombustible
            {
                Id = c.Id,
                VehiculoId = c.VehiculoId,
                TipoCombustibleId = c.TipoCombustibleId <= 0 ? (byte)1 : c.TipoCombustibleId,
                FechaHora = c.FechaHora == default ? DateTime.UtcNow : c.FechaHora,
                Litros = c.Litros,
                CostoTotal = c.CostoTotal,
                Odometro = c.Odometro,
                TanqueLleno = c.TanqueLleno,
                Gasolinera = c.Gasolinera?.Trim(),
                Notas = c.Notas?.Trim(),
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };

            await _sincronizacionRepository.GuardarCargaAsync(nuevaCarga, cancellationToken);

            if (c.Odometro > vehiculo.OdometroActual)
            {
                await _vehiculoRepository.ActualizarOdometroAsync(usuarioId.Value, vehiculo.Id, c.Odometro, cancellationToken);
                vehiculo.OdometroActual = c.Odometro;
            }

            respuesta.CargasInsertadas++;
            respuesta.CargasSincronizadasIds.Add(c.Id);
        }

        // 2. Procesar servicios offline
        foreach (var s in lote.Servicios)
        {
            if (s.Id == Guid.Empty)
                continue;

            var vehiculo = await ObtenerVehiculoValidoAsync(s.VehiculoId);
            if (vehiculo is null)
                continue;

            if (await _sincronizacionRepository.ExisteServicioAsync(s.Id, cancellationToken))
            {
                respuesta.ServiciosIgnoradosDuplicados++;
                respuesta.ServiciosSincronizadosIds.Add(s.Id);
                continue;
            }

            var nuevoServicio = new ServicioMantenimiento
            {
                Id = s.Id,
                VehiculoId = s.VehiculoId,
                TipoServicioId = s.TipoServicioId <= 0 ? (byte)1 : s.TipoServicioId,
                Fecha = s.Fecha == default ? DateOnly.FromDateTime(DateTime.UtcNow) : s.Fecha,
                KilometrajeServicio = s.KilometrajeServicio,
                Costo = s.Costo,
                Taller = s.Taller?.Trim(),
                Folio = s.Folio?.Trim(),
                Notas = s.Notas?.Trim(),
                MetaProximoKm = s.MetaProximoKm,
                FechaRetorno = s.FechaRetorno,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };

            await _sincronizacionRepository.GuardarServicioAsync(nuevoServicio, cancellationToken);

            if (s.KilometrajeServicio > vehiculo.OdometroActual)
            {
                await _vehiculoRepository.ActualizarOdometroAsync(usuarioId.Value, vehiculo.Id, s.KilometrajeServicio, cancellationToken);
                vehiculo.OdometroActual = s.KilometrajeServicio;
            }

            respuesta.ServiciosInsertados++;
            respuesta.ServiciosSincronizadosIds.Add(s.Id);
        }

        return ResultadoSincronizacion.CrearExito(respuesta);
    }
}
