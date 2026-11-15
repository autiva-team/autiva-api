using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para el resumen de gastos de un vehículo.
/// </summary>
public class GastoService : IGastoService
{
    private readonly IGastoRepository _gastoRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public GastoService(
        IGastoRepository gastoRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _gastoRepository = gastoRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoResumenGastos> ObtenerResumenAsync(
        Guid vehiculoId,
        int? anio,
        int? mes,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoResumenGastos.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoResumenGastos.CrearNoEncontrado("No se encontró el vehículo.");

        var errorParametros = ValidarAnioMes(anio, mes);

        if (errorParametros is not null)
            return ResultadoResumenGastos.CrearValidacion(errorParametros);

        var hoyMexico = ZonaMexico.Ahora();
        var anioConsulta = anio ?? hoyMexico.Year;
        var mesConsulta = mes ?? hoyMexico.Month;

        var errorMesFuturo = ValidarMesFuturo(anioConsulta, mesConsulta, hoyMexico);

        if (errorMesFuturo is not null)
            return ResultadoResumenGastos.CrearValidacion(errorMesFuturo);

        var (gastosMes, gastosMesAnterior) = await ObtenerGastosDelMesYAnteriorAsync(
            vehiculoId, anioConsulta, mesConsulta, cancellationToken);

        var totalMes = CalculadoraGastos.TotalMonto(gastosMes.Select(g => g.Monto ?? 0));
        var totalMesAnterior = CalculadoraGastos.TotalMonto(gastosMesAnterior.Select(g => g.Monto ?? 0));
        var diasDelPeriodo = CalcularDiasDelPeriodo(anioConsulta, mesConsulta, hoyMexico);

        var respuesta = new ResumenGastosResponse
        {
            VehiculoId = vehiculoId,
            Anio = anioConsulta,
            Mes = mesConsulta,
            TotalMes = totalMes,
            TotalMesAnterior = totalMesAnterior,
            VariacionPorcentaje = CalculadoraGastos.VariacionPorcentaje(totalMes, totalMesAnterior),
            CostoPromedioDiario = CalculadoraGastos.CostoPromedioDiario(totalMes, diasDelPeriodo),
            Desglose = CalculadoraGastos.Desglosar(gastosMes.Select(g => (g.Categoria, g.Monto ?? 0)), totalMes)
        };

        return ResultadoResumenGastos.CrearExito(respuesta);
    }

    private async Task<(IReadOnlyList<VwGastosConsolidados> GastosMes, IReadOnlyList<VwGastosConsolidados> GastosMesAnterior)>
        ObtenerGastosDelMesYAnteriorAsync(Guid vehiculoId, int anio, int mes, CancellationToken cancellationToken)
    {
        var primerDiaMes = new DateOnly(anio, mes, 1);
        var ultimoDiaMes = primerDiaMes.AddMonths(1).AddDays(-1);
        var primerDiaMesAnterior = primerDiaMes.AddMonths(-1);
        var ultimoDiaMesAnterior = primerDiaMes.AddDays(-1);

        var gastos = await _gastoRepository.ListarPorVehiculoYRangoAsync(
            vehiculoId, primerDiaMesAnterior, ultimoDiaMes, cancellationToken);

        var gastosMes = gastos.Where(g => g.Fecha >= primerDiaMes && g.Fecha <= ultimoDiaMes).ToList();
        var gastosMesAnterior = gastos.Where(g => g.Fecha >= primerDiaMesAnterior && g.Fecha <= ultimoDiaMesAnterior).ToList();

        return (gastosMes, gastosMesAnterior);
    }

    /// <summary>Días del mes consultado; si es el mes actual, solo los transcurridos hasta hoy (hora de México).</summary>
    private static int CalcularDiasDelPeriodo(int anio, int mes, DateTime hoyMexico) =>
        anio == hoyMexico.Year && mes == hoyMexico.Month
            ? hoyMexico.Day
            : DateTime.DaysInMonth(anio, mes);

    private static string? ValidarAnioMes(int? anio, int? mes)
    {
        if (anio.HasValue != mes.HasValue)
            return "Debe especificar año y mes juntos, o ninguno de los dos.";

        if (anio.HasValue && (anio < 1 || anio > 9999))
            return "El año no es válido.";

        if (mes.HasValue && (mes < 1 || mes > 12))
            return "El mes debe estar entre 1 y 12.";

        return null;
    }

    private static string? ValidarMesFuturo(int anio, int mes, DateTime hoyMexico)
    {
        var mesConsulta = new DateOnly(anio, mes, 1);
        var mesActual = new DateOnly(hoyMexico.Year, hoyMexico.Month, 1);

        return mesConsulta > mesActual ? "No se puede consultar un mes futuro." : null;
    }
}
