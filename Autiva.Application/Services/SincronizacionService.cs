using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para sincronizar en lote cargas y servicios capturados offline.
/// Aplica idempotencia por ID para evitar duplicados y las mismas reglas de validación
/// que el registro normal (HU-008 y HU-011); un registro inválido no detiene el lote.
/// </summary>
public class SincronizacionService : ISincronizacionService
{
    private readonly ISincronizacionRepository _sincronizacionRepository;
    private readonly ICargaRepository _cargaRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public SincronizacionService(
        ISincronizacionRepository sincronizacionRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService,
        ICargaRepository cargaRepository)
    {
        _sincronizacionRepository = sincronizacionRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
        _cargaRepository = cargaRepository;
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

        foreach (var c in lote.Cargas)
            await ProcesarCargaAsync(usuarioId.Value, c, vehiculosUsuario, respuesta, cancellationToken);

        foreach (var s in lote.Servicios)
            await ProcesarServicioAsync(usuarioId.Value, s, vehiculosUsuario, respuesta, cancellationToken);

        return ResultadoSincronizacion.CrearExito(respuesta);
    }

    private async Task<Vehiculo?> ObtenerVehiculoValidoAsync(
        Guid usuarioId, Guid vehiculoId, Dictionary<Guid, Vehiculo?> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(vehiculoId, out var vehiculo))
            return vehiculo;

        vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId, vehiculoId, cancellationToken);
        cache[vehiculoId] = vehiculo;
        return vehiculo;
    }

    private async Task ProcesarCargaAsync(
        Guid usuarioId,
        CargaSincronizacionDto c,
        Dictionary<Guid, Vehiculo?> vehiculosUsuario,
        SincronizacionLoteResponse respuesta,
        CancellationToken cancellationToken)
    {
        if (c.Id == Guid.Empty)
        {
            respuesta.Fallidos.Add(new SincronizacionItemFallido { Id = c.Id, Tipo = "carga", Motivo = "El id es obligatorio." });
            return;
        }

        var vehiculo = await ObtenerVehiculoValidoAsync(usuarioId, c.VehiculoId, vehiculosUsuario, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
        {
            respuesta.Fallidos.Add(new SincronizacionItemFallido { Id = c.Id, Tipo = "carga", Motivo = "No se encontró el vehículo." });
            return;
        }

        // Comprobar idempotencia: si ya existe el ID, se ignora la inserción sin error
        if (await _sincronizacionRepository.ExisteCargaAsync(c.Id, cancellationToken))
        {
            respuesta.CargasIgnoradasDuplicadas++;
            respuesta.CargasSincronizadasIds.Add(c.Id);
            return;
        }

        var fechaHora = c.FechaHora == default ? DateTime.UtcNow : c.FechaHora;
        var errores = ValidadorCarga.Validar(c.Litros, c.CostoTotal, c.Odometro, vehiculo.OdometroActual, fechaHora);

        if (errores.Count > 0)
        {
            respuesta.Fallidos.Add(new SincronizacionItemFallido { Id = c.Id, Tipo = "carga", Motivo = string.Join(" ", errores) });
            return;
        }

        var nuevaCarga = new CargaCombustible
        {
            Id = c.Id,
            VehiculoId = c.VehiculoId,
            TipoCombustibleId = c.TipoCombustibleId <= 0 ? (byte)1 : c.TipoCombustibleId,
            FechaHora = fechaHora,
            Litros = c.Litros,
            CostoTotal = c.CostoTotal,
            Odometro = c.Odometro,
            TanqueLleno = c.TanqueLleno,
            RendimientoKml = await CalcularRendimientoAsync(c, cancellationToken),
            Gasolinera = c.Gasolinera?.Trim(),
            Notas = c.Notas?.Trim(),
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var guardada = await _sincronizacionRepository.GuardarCargaAsync(nuevaCarga, cancellationToken);

        if (!guardada)
        {
            respuesta.Fallidos.Add(new SincronizacionItemFallido { Id = c.Id, Tipo = "carga", Motivo = "No se pudo guardar la carga." });
            return;
        }

        if (c.Odometro > vehiculo.OdometroActual)
        {
            await _vehiculoRepository.ActualizarOdometroAsync(usuarioId, vehiculo.Id, c.Odometro, cancellationToken);
            vehiculo.OdometroActual = c.Odometro;
        }

        respuesta.CargasInsertadas++;
        respuesta.CargasSincronizadasIds.Add(c.Id);
    }

    /// <summary>Rendimiento km/L de la carga, igual que en el registro normal (HU-008):
    /// solo si es de tanque lleno y hay una carga de tanque lleno anterior.</summary>
    private async Task<decimal?> CalcularRendimientoAsync(CargaSincronizacionDto c, CancellationToken cancellationToken)
    {
        if (!c.TanqueLleno)
            return null;

        var ultimaTanqueLleno = await _cargaRepository.ObtenerUltimaTanqueLlenoAsync(c.VehiculoId, cancellationToken);

        if (ultimaTanqueLleno is null)
            return null;

        var litrosPosteriores = await _cargaRepository.SumarLitrosPosterioresAsync(
            c.VehiculoId, ultimaTanqueLleno.FechaHora, cancellationToken);

        return CalculadoraRendimiento.Calcular(c.Odometro, ultimaTanqueLleno.Odometro, litrosPosteriores + c.Litros);
    }

    private async Task ProcesarServicioAsync(
        Guid usuarioId,
        ServicioSincronizacionDto s,
        Dictionary<Guid, Vehiculo?> vehiculosUsuario,
        SincronizacionLoteResponse respuesta,
        CancellationToken cancellationToken)
    {
        if (s.Id == Guid.Empty)
        {
            respuesta.Fallidos.Add(new SincronizacionItemFallido { Id = s.Id, Tipo = "servicio", Motivo = "El id es obligatorio." });
            return;
        }

        var vehiculo = await ObtenerVehiculoValidoAsync(usuarioId, s.VehiculoId, vehiculosUsuario, cancellationToken);

        if (vehiculo is null)
        {
            respuesta.Fallidos.Add(new SincronizacionItemFallido { Id = s.Id, Tipo = "servicio", Motivo = "No se encontró el vehículo." });
            return;
        }

        if (await _sincronizacionRepository.ExisteServicioAsync(s.Id, cancellationToken))
        {
            respuesta.ServiciosIgnoradosDuplicados++;
            respuesta.ServiciosSincronizadosIds.Add(s.Id);
            return;
        }

        var fecha = s.Fecha == default ? DateOnly.FromDateTime(DateTime.UtcNow) : s.Fecha;
        var errores = ValidadorServicio.Validar(s.Costo, s.KilometrajeServicio, vehiculo.KilometrajeInicial, s.MetaProximoKm, fecha, s.FechaRetorno);

        if (errores.Count > 0)
        {
            respuesta.Fallidos.Add(new SincronizacionItemFallido { Id = s.Id, Tipo = "servicio", Motivo = string.Join(" ", errores) });
            return;
        }

        var nuevoServicio = new ServicioMantenimiento
        {
            Id = s.Id,
            VehiculoId = s.VehiculoId,
            TipoServicioId = s.TipoServicioId <= 0 ? (byte)1 : s.TipoServicioId,
            Fecha = fecha,
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

        var guardado = await _sincronizacionRepository.GuardarServicioAsync(nuevoServicio, cancellationToken);

        if (!guardado)
        {
            respuesta.Fallidos.Add(new SincronizacionItemFallido { Id = s.Id, Tipo = "servicio", Motivo = "No se pudo guardar el servicio." });
            return;
        }

        if (s.KilometrajeServicio > vehiculo.OdometroActual)
        {
            await _vehiculoRepository.ActualizarOdometroAsync(usuarioId, vehiculo.Id, s.KilometrajeServicio, cancellationToken);
            vehiculo.OdometroActual = s.KilometrajeServicio;
        }

        respuesta.ServiciosInsertados++;
        respuesta.ServiciosSincronizadosIds.Add(s.Id);
    }
}
