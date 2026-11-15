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

        var errores = ValidadorVehiculo.Validar(marca, modelo, solicitud.Anio, placas, color, fotoUrl);

        if (solicitud.KilometrajeInicial < 0)
            errores.Add("El kilometraje inicial no puede ser negativo.");

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

    public async Task<ResultadoVehiculoDetalle> ActualizarOdometroAsync(
        Guid vehiculoId,
        ActualizarOdometroRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoVehiculoDetalle.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoVehiculoDetalle.CrearNoEncontrado("No se encontró el vehículo.");

        var nuevoOdometro = solicitud?.OdometroActual ?? 0;

        if (nuevoOdometro < 0)
            return ResultadoVehiculoDetalle.CrearValidacion("El odómetro no puede ser negativo.");

        if (nuevoOdometro < vehiculo.OdometroActual)
            return ResultadoVehiculoDetalle.CrearValidacion(
                $"El odómetro no puede ser menor al actual ({vehiculo.OdometroActual} km).");

        // Igual al actual: éxito sin tocar la base.
        if (nuevoOdometro == vehiculo.OdometroActual)
            return ResultadoVehiculoDetalle.CrearExito(MapearDetalle(vehiculo));

        var actualizado = await _vehiculoRepository.ActualizarOdometroAsync(
            usuarioId.Value, vehiculoId, nuevoOdometro, cancellationToken);

        return ResultadoVehiculoDetalle.CrearExito(MapearDetalle(actualizado));
    }

    public async Task<ResultadoEditarVehiculo> EditarAsync(
        Guid vehiculoId,
        EditarVehiculoRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new EditarVehiculoRequest();

        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoEditarVehiculo.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        // Mismo mensaje si no existe o si es de otro usuario: no se revela cuál es el caso.
        if (vehiculo is null)
            return ResultadoEditarVehiculo.CrearNoEncontrado("No se encontró el vehículo.");

        var marca = solicitud.Marca?.Trim() ?? string.Empty;
        var modelo = solicitud.Modelo?.Trim() ?? string.Empty;
        var placas = solicitud.Placas?.Trim().ToUpperInvariant() ?? string.Empty;
        var color = string.IsNullOrWhiteSpace(solicitud.Color) ? null : solicitud.Color.Trim();
        var fotoUrl = string.IsNullOrWhiteSpace(solicitud.FotoUrl) ? null : solicitud.FotoUrl.Trim();

        var errores = ValidadorVehiculo.Validar(marca, modelo, solicitud.Anio, placas, color, fotoUrl);

        if (!EsEstatusValido(solicitud.Estatus))
            errores.Add("El estatus debe ser \"activo\" o \"en_taller\".");

        if (errores.Count > 0)
            return ResultadoEditarVehiculo.CrearValidacion(errores);

        // Las placas del propio vehículo no cuentan como repetidas.
        var placasRepetidas = await _vehiculoRepository.ExistePlacasEnOtroVehiculoAsync(
            usuarioId.Value, placas, vehiculoId, cancellationToken);

        if (placasRepetidas)
            return ResultadoEditarVehiculo.CrearConflicto("Ya tienes un vehículo con esas placas.");

        var actualizado = await _vehiculoRepository.EditarAsync(
            usuarioId.Value, vehiculoId, marca, modelo, solicitud.Anio, placas, color, fotoUrl, solicitud.Estatus!,
            cancellationToken);

        return ResultadoEditarVehiculo.CrearExito(MapearDetalle(actualizado));
    }

    private static bool EsEstatusValido(string? estatus) =>
        estatus is "activo" or "en_taller";

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
}
