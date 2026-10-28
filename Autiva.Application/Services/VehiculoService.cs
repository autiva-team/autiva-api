using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para el registro de vehículos.
/// </summary>
public class VehiculoService : IVehiculoService
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public VehiculoService(
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoVehiculo> RegistrarAsync(
        RegistrarVehiculoRequest solicitud,
        CancellationToken cancellationToken)
    {
        solicitud ??= new RegistrarVehiculoRequest();

        // Seguridad: el vehículo siempre se liga al usuario del token.
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoVehiculo.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var marca = solicitud.Marca?.Trim() ?? string.Empty;
        var modelo = solicitud.Modelo?.Trim() ?? string.Empty;
        var placas = solicitud.Placas?.Trim().ToUpperInvariant() ?? string.Empty;
        var color = string.IsNullOrWhiteSpace(solicitud.Color) ? null : solicitud.Color.Trim();
        var fotoUrl = string.IsNullOrWhiteSpace(solicitud.FotoUrl) ? null : solicitud.FotoUrl.Trim();

        var errores = Validar(marca, modelo, solicitud.Anio, placas, solicitud.KilometrajeInicial, color, fotoUrl);

        if (errores.Count > 0)
            return ResultadoVehiculo.CrearValidacion(errores);

        var existePlacas = await _vehiculoRepository.ExistePlacasAsync(usuarioId.Value, placas, cancellationToken);

        if (existePlacas)
            return ResultadoVehiculo.CrearConflicto("Ya tienes un vehículo registrado con esas placas.");

        // El primer vehículo del usuario queda como el activo/principal.
        var esPrimero = !await _vehiculoRepository.TieneVehiculosAsync(usuarioId.Value, cancellationToken);

        var vehiculo = new Vehiculo
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId.Value,
            Marca = marca,
            Modelo = modelo,
            Anio = solicitud.Anio,
            Placas = placas,
            Color = color,
            KilometrajeInicial = solicitud.KilometrajeInicial,
            OdometroActual = solicitud.KilometrajeInicial,
            Estatus = "activo",
            EsPrincipal = esPrimero,
            FotoUrl = fotoUrl,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var agregado = await _vehiculoRepository.AgregarAsync(vehiculo, cancellationToken);

        if (!agregado)
            return ResultadoVehiculo.CrearConflicto("Ya tienes un vehículo registrado con esas placas.");

        var respuesta = new VehiculoResponse
        {
            Id = vehiculo.Id,
            Marca = vehiculo.Marca,
            Modelo = vehiculo.Modelo,
            Anio = vehiculo.Anio,
            Placas = vehiculo.Placas,
            Color = vehiculo.Color,
            KilometrajeInicial = vehiculo.KilometrajeInicial,
            OdometroActual = vehiculo.OdometroActual,
            Estatus = vehiculo.Estatus,
            EsPrincipal = vehiculo.EsPrincipal,
            FotoUrl = vehiculo.FotoUrl,
            CreadoEn = vehiculo.CreadoEn
        };

        return ResultadoVehiculo.CrearExito(respuesta);
    }

    public async Task<ResultadoListaVehiculos> ListarAsync(CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoListaVehiculos.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculos = await _vehiculoRepository.ListarAsync(usuarioId.Value, cancellationToken);

        var datos = vehiculos.Select(MapearResumen).ToList();

        return ResultadoListaVehiculos.CrearExito(datos);
    }

    public async Task<ResultadoVehiculoDetalle> ObtenerDetalleAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoVehiculoDetalle.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoVehiculoDetalle.CrearNoEncontrado("No se encontró el vehículo.");

        return ResultadoVehiculoDetalle.CrearExito(MapearDetalle(vehiculo));
    }

    public async Task<ResultadoVehiculoDetalle> MarcarActivoAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoVehiculoDetalle.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoVehiculoDetalle.CrearNoEncontrado("No se encontró el vehículo.");

        // Ya es el activo: éxito sin tocar nada.
        if (vehiculo.EsPrincipal)
            return ResultadoVehiculoDetalle.CrearExito(MapearDetalle(vehiculo));

        var actualizado = await _vehiculoRepository.MarcarActivoAsync(usuarioId.Value, vehiculoId, cancellationToken);

        return ResultadoVehiculoDetalle.CrearExito(MapearDetalle(actualizado));
    }

    private static VehiculoResumenDto MapearResumen(Vehiculo vehiculo) => new()
    {
        Id = vehiculo.Id,
        Marca = vehiculo.Marca,
        Modelo = vehiculo.Modelo,
        Placas = vehiculo.Placas,
        OdometroActual = vehiculo.OdometroActual,
        Estatus = vehiculo.Estatus,
        EsPrincipal = vehiculo.EsPrincipal
    };

    private static VehiculoResponse MapearDetalle(Vehiculo vehiculo) => new()
    {
        Id = vehiculo.Id,
        Marca = vehiculo.Marca,
        Modelo = vehiculo.Modelo,
        Anio = vehiculo.Anio,
        Placas = vehiculo.Placas,
        Color = vehiculo.Color,
        KilometrajeInicial = vehiculo.KilometrajeInicial,
        OdometroActual = vehiculo.OdometroActual,
        Estatus = vehiculo.Estatus,
        EsPrincipal = vehiculo.EsPrincipal,
        FotoUrl = vehiculo.FotoUrl,
        CreadoEn = vehiculo.CreadoEn
    };

    private static List<string> Validar(
        string marca,
        string modelo,
        short anio,
        string placas,
        int kilometrajeInicial,
        string? color,
        string? fotoUrl)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(marca))
            errores.Add("La marca es obligatoria.");
        else if (marca.Length > 50)
            errores.Add("La marca no puede superar los 50 caracteres.");

        if (string.IsNullOrWhiteSpace(modelo))
            errores.Add("El modelo es obligatorio.");
        else if (modelo.Length > 50)
            errores.Add("El modelo no puede superar los 50 caracteres.");

        var anioMaximo = DateTime.UtcNow.Year + 1;

        if (anio < 1900 || anio > anioMaximo)
            errores.Add($"El año debe estar entre 1900 y {anioMaximo}.");

        if (string.IsNullOrWhiteSpace(placas))
            errores.Add("Las placas son obligatorias.");
        else if (placas.Length > 15)
            errores.Add("Las placas no pueden superar los 15 caracteres.");

        if (kilometrajeInicial < 0)
            errores.Add("El kilometraje inicial no puede ser negativo.");

        if (color is { Length: > 30 })
            errores.Add("El color no puede superar los 30 caracteres.");

        if (fotoUrl is { Length: > 500 })
            errores.Add("La URL de la foto no puede superar los 500 caracteres.");

        return errores;
    }
}
