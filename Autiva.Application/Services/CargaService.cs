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

        return ResultadoCarga.CrearExito(MapearRespuesta(guardada, tipoCombustible!.Nombre, kmRecorridos));
    }

    public async Task<ResultadoHistorialCargas> ObtenerHistorialAsync(
        Guid vehiculoId,
        int pagina,
        int tamano,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoHistorialCargas.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe, está eliminado o es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoHistorialCargas.CrearNoEncontrado("No se encontró el vehículo.");

        var errorFechas = ValidarFechas(desde, hasta);

        if (errorFechas is not null)
            return ResultadoHistorialCargas.CrearValidacion(errorFechas);

        (pagina, tamano) = NormalizarPaginacion(pagina, tamano);

        var (cargas, totalRegistros) = await _cargaRepository.ListarHistorialAsync(
            vehiculoId, pagina, tamano, desde, hasta, cancellationToken);

        var items = await MapearHistorialAsync(vehiculoId, cargas, cancellationToken);

        var datos = new ResultadoPaginado<CargaResponse>
        {
            Items = items,
            Pagina = pagina,
            Tamano = tamano,
            TotalRegistros = totalRegistros,
            TotalPaginas = CalcularTotalPaginas(totalRegistros, tamano)
        };

        return ResultadoHistorialCargas.CrearExito(datos);
    }

    public async Task<ResultadoCarga> ObtenerDetalleAsync(Guid cargaId, CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoCarga.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var carga = await _cargaRepository.ObtenerPorUsuarioAsync(usuarioId.Value, cargaId, cancellationToken);

        // Mismo mensaje si no existe, está eliminada o es de otro usuario: no se revela cuál es el caso.
        if (carga is null)
            return ResultadoCarga.CrearNoEncontrado("No se encontró la carga.");

        var anterior = await _cargaRepository.ObtenerAnteriorAsync(
            carga.VehiculoId, carga.FechaHora, carga.Odometro, cancellationToken);

        return ResultadoCarga.CrearExito(MapearRespuesta(carga, carga.TipoCombustible.Nombre, CalcularKmRecorridos(carga, anterior)));
    }

    /// <summary>
    /// Mapea cada carga a su <see cref="CargaResponse"/> calculando los km recorridos contra
    /// la anterior. Las cargas vienen de más reciente a más antigua: la anterior de cada una
    /// es la siguiente en esta misma lista, salvo la última de la página, que puede estar en
    /// la página siguiente y hay que buscarla en la base.
    /// </summary>
    private async Task<List<CargaResponse>> MapearHistorialAsync(
        Guid vehiculoId, IReadOnlyList<CargaCombustible> cargas, CancellationToken cancellationToken)
    {
        var items = new List<CargaResponse>(cargas.Count);

        for (var i = 0; i < cargas.Count; i++)
        {
            var anterior = i + 1 < cargas.Count
                ? cargas[i + 1]
                : await _cargaRepository.ObtenerAnteriorAsync(
                    vehiculoId, cargas[i].FechaHora, cargas[i].Odometro, cancellationToken);

            items.Add(MapearRespuesta(cargas[i], cargas[i].TipoCombustible.Nombre, CalcularKmRecorridos(cargas[i], anterior)));
        }

        return items;
    }

    private static CargaResponse MapearRespuesta(CargaCombustible carga, string tipoCombustibleNombre, int? kmRecorridos) => new()
    {
        Id = carga.Id,
        VehiculoId = carga.VehiculoId,
        TipoCombustible = tipoCombustibleNombre,
        FechaHora = carga.FechaHora,
        Litros = carga.Litros,
        CostoTotal = carga.CostoTotal,
        PrecioPorLitro = Math.Round(carga.CostoTotal / carga.Litros, 2),
        Odometro = carga.Odometro,
        KmRecorridos = kmRecorridos,
        TanqueLleno = carga.TanqueLleno,
        RendimientoKml = carga.RendimientoKml,
        Gasolinera = carga.Gasolinera,
        Notas = carga.Notas
    };

    private static int? CalcularKmRecorridos(CargaCombustible carga, CargaCombustible? anterior) =>
        anterior is null ? null : carga.Odometro - anterior.Odometro;

    /// <summary>Error si "desde" es mayor a "hasta"; null si las fechas son válidas.</summary>
    private static string? ValidarFechas(DateTime? desde, DateTime? hasta) =>
        desde is not null && hasta is not null && desde > hasta
            ? "La fecha 'desde' no puede ser mayor a 'hasta'."
            : null;

    private static (int Pagina, int Tamano) NormalizarPaginacion(int pagina, int tamano) =>
        (Math.Max(pagina, 1), Math.Clamp(tamano, 1, 50));

    private static int CalcularTotalPaginas(int totalRegistros, int tamano) =>
        totalRegistros == 0 ? 0 : (int)Math.Ceiling(totalRegistros / (double)tamano);

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
