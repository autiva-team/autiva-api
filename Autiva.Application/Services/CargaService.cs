using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para el registro de cargas de combustible.
/// </summary>
public class CargaService : ICargaService
{
    private readonly ICargaRepository _cargaRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public CargaService(
        ICargaRepository cargaRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _cargaRepository = cargaRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoCarga> RegistrarAsync(
        Guid vehiculoId,
        RegistrarCargaRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new RegistrarCargaRequest();

        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoCarga.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoCarga.CrearNoEncontrado("No se encontró el vehículo.");

        var id = solicitud.Id ?? Guid.NewGuid();
        var fechaHora = solicitud.FechaHora ?? DateTime.UtcNow;
        var gasolinera = string.IsNullOrWhiteSpace(solicitud.Gasolinera) ? null : solicitud.Gasolinera.Trim();
        var folio = string.IsNullOrWhiteSpace(solicitud.Folio) ? null : solicitud.Folio.Trim();
        var notas = string.IsNullOrWhiteSpace(solicitud.Notas) ? null : solicitud.Notas.Trim();

        var errores = ValidarCamposBasicos(solicitud.Litros, solicitud.CostoTotal, solicitud.Odometro, vehiculo.OdometroActual, fechaHora);

        var tipoCombustible = await _cargaRepository.ObtenerTipoCombustibleAsync(solicitud.TipoCombustibleId, cancellationToken);

        if (tipoCombustible is null)
            errores.Add("El tipo de combustible no existe.");

        if (errores.Count > 0)
            return ResultadoCarga.CrearValidacion(errores);

        var existeId = await _cargaRepository.ExisteAsync(id, cancellationToken);

        if (existeId)
            return ResultadoCarga.CrearConflicto("La carga ya fue registrada.");

        // Km recorridos: contra la carga más reciente del vehículo (de cualquier tipo).
        var ultimaCarga = await _cargaRepository.ObtenerUltimaAsync(vehiculoId, cancellationToken);
        int? kmRecorridos = ultimaCarga is null ? null : solicitud.Odometro - ultimaCarga.Odometro;

        // Rendimiento: solo si esta carga es de tanque lleno y hay una carga de tanque lleno anterior.
        decimal? rendimientoKml = null;

        if (solicitud.TanqueLleno)
        {
            var ultimaTanqueLleno = await _cargaRepository.ObtenerUltimaTanqueLlenoAsync(vehiculoId, cancellationToken);

            if (ultimaTanqueLleno is not null)
            {
                var litrosPosteriores = await _cargaRepository.SumarLitrosPosterioresAsync(
                    vehiculoId, ultimaTanqueLleno.FechaHora, cancellationToken);

                rendimientoKml = CalculadoraRendimiento.Calcular(
                    solicitud.Odometro, ultimaTanqueLleno.Odometro, litrosPosteriores + solicitud.Litros);
            }
        }

        var carga = new CargaCombustible
        {
            Id = id,
            VehiculoId = vehiculoId,
            TipoCombustibleId = solicitud.TipoCombustibleId,
            FechaHora = fechaHora,
            Litros = solicitud.Litros,
            CostoTotal = solicitud.CostoTotal,
            Odometro = solicitud.Odometro,
            TanqueLleno = solicitud.TanqueLleno,
            RendimientoKml = rendimientoKml,
            Gasolinera = gasolinera,
            Folio = folio,
            Notas = notas,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var guardada = await _cargaRepository.RegistrarAsync(carga, vehiculoId, cancellationToken);

        if (guardada is null)
            return ResultadoCarga.CrearConflicto("La carga ya fue registrada.");

        var respuesta = new CargaResponse
        {
            Id = guardada.Id,
            VehiculoId = guardada.VehiculoId,
            TipoCombustible = tipoCombustible!.Nombre,
            FechaHora = guardada.FechaHora,
            Litros = guardada.Litros,
            CostoTotal = guardada.CostoTotal,
            PrecioPorLitro = Math.Round(guardada.CostoTotal / guardada.Litros, 2),
            Odometro = guardada.Odometro,
            KmRecorridos = kmRecorridos,
            TanqueLleno = guardada.TanqueLleno,
            RendimientoKml = guardada.RendimientoKml,
            Gasolinera = guardada.Gasolinera,
            Notas = guardada.Notas
        };

        return ResultadoCarga.CrearExito(respuesta);
    }

    private static List<string> ValidarCamposBasicos(
        decimal litros,
        decimal costoTotal,
        int odometro,
        int odometroActualVehiculo,
        DateTime fechaHora)
    {
        var errores = new List<string>();

        if (litros <= 0)
            errores.Add("Los litros deben ser mayores a cero.");

        if (costoTotal <= 0)
            errores.Add("El costo total debe ser mayor a cero.");

        if (odometro < odometroActualVehiculo)
            errores.Add($"El odómetro no puede ser menor al actual ({odometroActualVehiculo} km).");

        // Tolerancia de 5 minutos por diferencia de reloj entre el dispositivo y el servidor.
        if (fechaHora > DateTime.UtcNow.AddMinutes(5))
            errores.Add("La fecha y hora de la carga no puede estar en el futuro.");

        return errores;
    }
}
