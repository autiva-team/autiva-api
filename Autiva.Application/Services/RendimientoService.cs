using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio del rendimiento (km/L) y gasto de combustible de un vehículo.
/// Usa <see cref="CalculadoraRendimiento"/> para los cálculos; el mismo cálculo lo
/// reutilizan el dashboard (HU-013) y las estadísticas.
/// </summary>
public class RendimientoService : IRendimientoService
{
    private static readonly TimeZoneInfo ZonaHorariaMexico =
        TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    private readonly ICargaRepository _cargaRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public RendimientoService(
        ICargaRepository cargaRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _cargaRepository = cargaRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoRendimiento> ObtenerAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoRendimiento.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe, está eliminado o es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoRendimiento.CrearNoEncontrado("No se encontró el vehículo.");

        var cargas = await _cargaRepository.ListarNoEliminadasAsync(vehiculoId, cancellationToken);

        // Los meses se calculan en hora de México, no en UTC: una carga hecha a las
        // 23:30 UTC del día 1 ya es "mes actual" en hora de México si cae el día 2.
        var ahoraMexico = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaHorariaMexico);
        var mesActual = (ahoraMexico.Year, ahoraMexico.Month);
        var primerDiaMesAnterior = new DateTime(ahoraMexico.Year, ahoraMexico.Month, 1).AddMonths(-1);
        var mesAnterior = (primerDiaMesAnterior.Year, primerDiaMesAnterior.Month);

        var rendimientos = new List<decimal>();
        var rendimientosMesActual = new List<decimal>();
        var rendimientosMesAnterior = new List<decimal>();
        var gastoMesActual = 0m;

        foreach (var carga in cargas)
        {
            var fechaMexico = ConvertirAMexico(carga.FechaHora);
            var mesCarga = (fechaMexico.Year, fechaMexico.Month);

            if (carga.RendimientoKml.HasValue)
            {
                rendimientos.Add(carga.RendimientoKml.Value);

                if (mesCarga == mesActual)
                    rendimientosMesActual.Add(carga.RendimientoKml.Value);
                else if (mesCarga == mesAnterior)
                    rendimientosMesAnterior.Add(carga.RendimientoKml.Value);
            }

            if (mesCarga == mesActual)
                gastoMesActual += carga.CostoTotal;
        }

        var rendimientoPromedioKml = CalculadoraRendimiento.Promedio(rendimientos);
        var promedioMesActual = CalculadoraRendimiento.Promedio(rendimientosMesActual);
        var promedioMesAnterior = CalculadoraRendimiento.Promedio(rendimientosMesAnterior);

        var respuesta = new RendimientoResponse
        {
            RendimientoPromedioKml = rendimientoPromedioKml,
            VariacionMesAnteriorPorcentaje = CalculadoraRendimiento.VariacionPorcentaje(promedioMesActual, promedioMesAnterior),
            GastoMesActual = gastoMesActual,
            AutonomiaEstimadaKm = CalculadoraRendimiento.AutonomiaEstimada(rendimientoPromedioKml, vehiculo.CapacidadTanqueL),
            TotalCargas = cargas.Count
        };

        return ResultadoRendimiento.CrearExito(respuesta);
    }

    private static DateTime ConvertirAMexico(DateTime fechaHoraUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaHoraUtc, DateTimeKind.Utc), ZonaHorariaMexico);
}
