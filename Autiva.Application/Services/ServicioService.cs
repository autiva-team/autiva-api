using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para el registro de servicios de mantenimiento.
/// </summary>
public class ServicioService : IServicioService
{
    private readonly IServicioRepository _servicioRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public ServicioService(
        IServicioRepository servicioRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _servicioRepository = servicioRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoServicio> RegistrarAsync(
        Guid vehiculoId,
        RegistrarServicioRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new RegistrarServicioRequest();

        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoServicio.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoServicio.CrearNoEncontrado("No se encontró el vehículo.");

        var id = solicitud.Id ?? Guid.NewGuid();
        var taller = string.IsNullOrWhiteSpace(solicitud.Taller) ? null : solicitud.Taller.Trim();
        var folio = string.IsNullOrWhiteSpace(solicitud.Folio) ? null : solicitud.Folio.Trim();
        var notas = string.IsNullOrWhiteSpace(solicitud.Notas) ? null : solicitud.Notas.Trim();

        var errores = ValidarCampos(solicitud, vehiculo.KilometrajeInicial);

        var tipoServicio = await _servicioRepository.ObtenerTipoServicioAsync(solicitud.TipoServicioId, cancellationToken);

        if (tipoServicio is null)
            errores.Add("El tipo de servicio no existe.");

        if (errores.Count > 0)
            return ResultadoServicio.CrearValidacion(errores);

        var existeId = await _servicioRepository.ExisteAsync(id, cancellationToken);

        if (existeId)
            return ResultadoServicio.CrearConflicto("El servicio ya fue registrado.");

        var servicio = new ServicioMantenimiento
        {
            Id = id,
            VehiculoId = vehiculoId,
            TipoServicioId = solicitud.TipoServicioId,
            Fecha = solicitud.Fecha,
            KilometrajeServicio = solicitud.KilometrajeServicio,
            Costo = solicitud.Costo,
            MetaProximoKm = solicitud.MetaProximoKm,
            FechaRetorno = solicitud.FechaRetorno,
            Taller = taller,
            Folio = folio,
            Notas = notas,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var guardado = await _servicioRepository.RegistrarAsync(servicio, vehiculoId, cancellationToken);

        if (guardado is null)
            return ResultadoServicio.CrearConflicto("El servicio ya fue registrado.");

        return ResultadoServicio.CrearExito(MapearRespuesta(guardado, tipoServicio!.Nombre));
    }

    public async Task<ResultadoHistorialServicios> ObtenerHistorialAsync(
        Guid vehiculoId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoHistorialServicios.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe, está eliminado o es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoHistorialServicios.CrearNoEncontrado("No se encontró el vehículo.");

        (pagina, tamano) = NormalizarPaginacion(pagina, tamano);

        var (servicios, totalRegistros) = await _servicioRepository.ListarHistorialAsync(
            vehiculoId, pagina, tamano, cancellationToken);

        var datos = new ResultadoPaginado<ServicioResponse>
        {
            Items = servicios.Select(s => MapearRespuesta(s, s.TipoServicio.Nombre)).ToList(),
            Pagina = pagina,
            Tamano = tamano,
            TotalRegistros = totalRegistros,
            TotalPaginas = CalcularTotalPaginas(totalRegistros, tamano)
        };

        return ResultadoHistorialServicios.CrearExito(datos);
    }

    public async Task<ResultadoServicio> ObtenerDetalleAsync(Guid servicioId, CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoServicio.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var servicio = await _servicioRepository.ObtenerPorUsuarioAsync(usuarioId.Value, servicioId, cancellationToken);

        // Mismo mensaje si no existe, está eliminado o es de otro usuario: no se revela cuál es el caso.
        if (servicio is null)
            return ResultadoServicio.CrearNoEncontrado("No se encontró el servicio.");

        return ResultadoServicio.CrearExito(MapearRespuesta(servicio, servicio.TipoServicio.Nombre));
    }

    private static (int Pagina, int Tamano) NormalizarPaginacion(int pagina, int tamano) =>
        (Math.Max(pagina, 1), Math.Clamp(tamano, 1, 50));

    private static int CalcularTotalPaginas(int totalRegistros, int tamano) =>
        totalRegistros == 0 ? 0 : (int)Math.Ceiling(totalRegistros / (double)tamano);

    private static ServicioResponse MapearRespuesta(ServicioMantenimiento servicio, string tipoServicioNombre) => new()
    {
        Id = servicio.Id,
        VehiculoId = servicio.VehiculoId,
        TipoServicio = tipoServicioNombre,
        Fecha = servicio.Fecha,
        KilometrajeServicio = servicio.KilometrajeServicio,
        Costo = servicio.Costo,
        MetaProximoKm = servicio.MetaProximoKm,
        FechaRetorno = servicio.FechaRetorno,
        Taller = servicio.Taller,
        Folio = servicio.Folio,
        Notas = servicio.Notas
    };

    private static List<string> ValidarCampos(RegistrarServicioRequest solicitud, int kilometrajeInicialVehiculo)
    {
        var errores = new List<string>();

        if (solicitud.Costo < 0)
            errores.Add("El costo no puede ser negativo.");

        if (solicitud.KilometrajeServicio < 0)
            errores.Add("El kilometraje del servicio no puede ser negativo.");
        else if (solicitud.KilometrajeServicio < kilometrajeInicialVehiculo)
            errores.Add($"El kilometraje del servicio no puede ser menor al inicial del vehículo ({kilometrajeInicialVehiculo} km).");

        if (solicitud.MetaProximoKm is not null && solicitud.MetaProximoKm <= solicitud.KilometrajeServicio)
            errores.Add("La meta del próximo servicio debe ser mayor al kilometraje del servicio.");

        if (solicitud.FechaRetorno is not null && solicitud.FechaRetorno < solicitud.Fecha)
            errores.Add("La fecha de retorno no puede ser anterior a la fecha del servicio.");

        if (solicitud.Fecha > DateOnly.FromDateTime(DateTime.UtcNow))
            errores.Add("La fecha del servicio no puede estar en el futuro.");

        return errores;
    }
}
