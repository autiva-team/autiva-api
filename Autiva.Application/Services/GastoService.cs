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

    public async Task<ResultadoGasto> RegistrarAsync(
        Guid vehiculoId,
        RegistrarGastoRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new RegistrarGastoRequest();

        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoGasto.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoGasto.CrearNoEncontrado("No se encontró el vehículo.");

        var id = solicitud.Id ?? Guid.NewGuid();
        var concepto = (solicitud.Concepto ?? string.Empty).Trim();
        var fecha = solicitud.Fecha ?? DateOnly.FromDateTime(ZonaMexico.Ahora());

        var errores = ValidadorGasto.Validar(concepto, solicitud.Monto, fecha, solicitud.Odometro);

        var categoria = await _gastoRepository.ObtenerCategoriaAsync(solicitud.CategoriaId, cancellationToken);

        if (categoria is null)
            errores.Add("La categoría no existe.");

        if (errores.Count > 0)
            return ResultadoGasto.CrearValidacion(errores);

        var existeId = await _gastoRepository.ExisteAsync(id, cancellationToken);

        if (existeId)
            return ResultadoGasto.CrearConflicto("El gasto ya fue registrado.");

        var gasto = new Gasto
        {
            Id = id,
            VehiculoId = vehiculoId,
            CategoriaId = solicitud.CategoriaId,
            Concepto = concepto,
            Monto = solicitud.Monto,
            Fecha = fecha,
            Odometro = solicitud.Odometro,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var guardado = await _gastoRepository.RegistrarAsync(gasto, cancellationToken);

        if (guardado is null)
            return ResultadoGasto.CrearConflicto("El gasto ya fue registrado.");

        // Un odómetro menor al actual se acepta sin tocarlo (puede ser un gasto viejo).
        if (solicitud.Odometro is int odometro && odometro > vehiculo.OdometroActual)
            await _vehiculoRepository.ActualizarOdometroAsync(usuarioId.Value, vehiculoId, odometro, cancellationToken);

        return ResultadoGasto.CrearExito(MapearRespuesta(guardado, categoria!.Nombre));
    }

    public async Task<ResultadoHistorialGastos> ListarAsync(
        Guid vehiculoId,
        int? anio,
        int? mes,
        byte? categoriaId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoHistorialGastos.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        if (vehiculo is null)
            return ResultadoHistorialGastos.CrearNoEncontrado("No se encontró el vehículo.");

        var errorAnioMes = ValidarAnioMes(anio, mes);

        if (errorAnioMes is not null)
            return ResultadoHistorialGastos.CrearValidacion(errorAnioMes);

        if (categoriaId is not null)
        {
            var categoria = await _gastoRepository.ObtenerCategoriaAsync(categoriaId.Value, cancellationToken);

            if (categoria is null)
                return ResultadoHistorialGastos.CrearValidacion("La categoría no existe.");
        }

        (pagina, tamano) = NormalizarPaginacion(pagina, tamano);

        var (gastos, totalRegistros) = await _gastoRepository.ListarHistorialAsync(
            vehiculoId, pagina, tamano, anio, mes, categoriaId, cancellationToken);

        var datos = new ResultadoPaginado<GastoResponse>
        {
            Items = gastos.Select(g => MapearRespuesta(g, g.Categoria.Nombre)).ToList(),
            Pagina = pagina,
            Tamano = tamano,
            TotalRegistros = totalRegistros,
            TotalPaginas = CalcularTotalPaginas(totalRegistros, tamano)
        };

        return ResultadoHistorialGastos.CrearExito(datos);
    }

    private static GastoResponse MapearRespuesta(Gasto gasto, string categoriaNombre) => new()
    {
        Id = gasto.Id,
        VehiculoId = gasto.VehiculoId,
        Categoria = categoriaNombre,
        Concepto = gasto.Concepto,
        Monto = gasto.Monto,
        Fecha = gasto.Fecha,
        Odometro = gasto.Odometro
    };

    private static (int Pagina, int Tamano) NormalizarPaginacion(int pagina, int tamano) =>
        (Math.Max(pagina, 1), Math.Clamp(tamano, 1, 50));

    private static int CalcularTotalPaginas(int totalRegistros, int tamano) =>
        totalRegistros == 0 ? 0 : (int)Math.Ceiling(totalRegistros / (double)tamano);

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
