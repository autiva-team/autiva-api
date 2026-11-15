using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio de las estadísticas de costo y eficiencia de un vehículo (HU-028).
/// </summary>
public class EstadisticasService : IEstadisticasService
{
    private readonly IGastoRepository _gastoRepository;
    private readonly ICargaRepository _cargaRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public EstadisticasService(
        IGastoRepository gastoRepository,
        ICargaRepository cargaRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _gastoRepository = gastoRepository;
        _cargaRepository = cargaRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoEstadisticas> ObtenerAsync(
        Guid vehiculoId,
        string? periodo,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoEstadisticas.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe, está eliminado o es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoEstadisticas.CrearNoEncontrado("No se encontró el vehículo.");

        var periodoNormalizado = string.IsNullOrWhiteSpace(periodo) ? "1M" : periodo.Trim().ToUpperInvariant();
        var mesesPeriodo = CalculadoraEstadisticas.ObtenerMesesDelPeriodo(periodoNormalizado);

        if (mesesPeriodo is null)
            return ResultadoEstadisticas.CrearValidacion("El periodo debe ser 1M, 3M, 6M o 1A.");

        var hoyMexico = ZonaMexico.Ahora();
        var hasta = DateOnly.FromDateTime(hoyMexico);
        var desde = hasta.AddMonths(-mesesPeriodo.Value);

        var (gastoTotal, distanciaKm, costoPorKm) = await ObtenerCostoYDistanciaAsync(vehiculoId, desde, hasta, cancellationToken);

        var hastaAnterior = desde.AddDays(-1);
        var desdeAnterior = desde.AddMonths(-mesesPeriodo.Value);
        var (_, _, costoPorKmAnterior) = await ObtenerCostoYDistanciaAsync(vehiculoId, desdeAnterior, hastaAnterior, cancellationToken);

        var cargas = await _cargaRepository.ListarNoEliminadasAsync(vehiculoId, cancellationToken);

        var primerDiaMesActual = new DateOnly(hoyMexico.Year, hoyMexico.Month, 1);
        var (gastoMesActual, _, _) = await ObtenerCostoYDistanciaAsync(vehiculoId, primerDiaMesActual, hasta, cancellationToken);
        var gastoProyectadoMes = CalculadoraEstadisticas.CalcularGastoProyectadoMes(
            gastoMesActual, hoyMexico.Day, DateTime.DaysInMonth(hoyMexico.Year, hoyMexico.Month));

        var desdeUltimos30Dias = hasta.AddDays(-29);
        var (_, distanciaUltimos30Dias, _) = await ObtenerCostoYDistanciaAsync(vehiculoId, desdeUltimos30Dias, hasta, cancellationToken);

        var respuesta = new EstadisticasResponse
        {
            Periodo = periodoNormalizado,
            Desde = desde,
            Hasta = hasta,
            GastoTotal = gastoTotal,
            DistanciaKm = distanciaKm,
            CostoPorKm = costoPorKm,
            VariacionCostoPorKm = CalculadoraRendimiento.VariacionPorcentaje(costoPorKm, costoPorKmAnterior),
            RendimientoMensual = CalcularRendimientoMensual(cargas, desde, hasta),
            RendimientoPromedio = CalculadoraRendimiento.Promedio(ObtenerRendimientosDelPeriodo(cargas, desde, hasta)),
            GastoProyectadoMes = gastoProyectadoMes,
            DistanciaUltimos30Dias = distanciaUltimos30Dias
        };

        return ResultadoEstadisticas.CrearExito(respuesta);
    }

    /// <summary>
    /// Gasto total, distancia y costo por km de un rango: consulta la vista de gastos
    /// consolidados dentro del rango y antes de él, para la regla de distancia.
    /// </summary>
    private async Task<(decimal GastoTotal, int? DistanciaKm, decimal? CostoPorKm)> ObtenerCostoYDistanciaAsync(
        Guid vehiculoId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var dentro = await _gastoRepository.ListarPorVehiculoYRangoAsync(vehiculoId, desde, hasta, cancellationToken);
        var antes = await _gastoRepository.ListarPorVehiculoYRangoAsync(vehiculoId, DateOnly.MinValue, desde.AddDays(-1), cancellationToken);

        var gastoTotal = CalculadoraGastos.TotalMonto(dentro.Select(g => g.Monto ?? 0));
        var distanciaKm = CalculadoraEstadisticas.CalcularDistanciaKm(
            dentro.Where(g => g.Odometro.HasValue).Select(g => g.Odometro!.Value),
            antes.Where(g => g.Odometro.HasValue).Select(g => g.Odometro!.Value));
        var costoPorKm = CalculadoraEstadisticas.CalcularCostoPorKm(gastoTotal, distanciaKm);

        return (gastoTotal, distanciaKm, costoPorKm);
    }

    private static List<RendimientoMensualDto> CalcularRendimientoMensual(
        IReadOnlyList<CargaCombustible> cargas, DateOnly desde, DateOnly hasta) =>
        CalculadoraEstadisticas.ListarMesesDelPeriodo(desde, hasta)
            .Select(mes => new RendimientoMensualDto
            {
                Anio = mes.Anio,
                Mes = mes.Mes,
                KmL = CalculadoraRendimiento.Promedio(
                    cargas.Where(c => c.RendimientoKml.HasValue && EstaEnMes(c.FechaHora, mes.Anio, mes.Mes))
                          .Select(c => c.RendimientoKml!.Value)
                          .ToList())
            })
            .ToList();

    private static List<decimal> ObtenerRendimientosDelPeriodo(
        IReadOnlyList<CargaCombustible> cargas, DateOnly desde, DateOnly hasta) =>
        cargas
            .Where(c => c.RendimientoKml.HasValue && EstaEnRango(c.FechaHora, desde, hasta))
            .Select(c => c.RendimientoKml!.Value)
            .ToList();

    private static bool EstaEnMes(DateTime fechaHoraUtc, int anio, int mes)
    {
        var fechaMexico = ZonaMexico.ConvertirDesdeUtc(fechaHoraUtc);
        return fechaMexico.Year == anio && fechaMexico.Month == mes;
    }

    private static bool EstaEnRango(DateTime fechaHoraUtc, DateOnly desde, DateOnly hasta)
    {
        var fechaMexico = DateOnly.FromDateTime(ZonaMexico.ConvertirDesdeUtc(fechaHoraUtc));
        return fechaMexico >= desde && fechaMexico <= hasta;
    }
}
