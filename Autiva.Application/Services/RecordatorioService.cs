using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Lógica de negocio para recordatorios y resumen del vehículo.
/// </summary>
public class RecordatorioService : IRecordatorioService
{
    private static readonly HashSet<string> TiposValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "seguro", "verificacion", "mantenimiento", "tenencia", "licencia", "otro"
    };

    private readonly IRecordatorioRepository _recordatorioRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly ICurrentUserService _currentUserService;

    public RecordatorioService(
        IRecordatorioRepository recordatorioRepository,
        IVehiculoRepository vehiculoRepository,
        ICurrentUserService currentUserService)
    {
        _recordatorioRepository = recordatorioRepository;
        _vehiculoRepository = vehiculoRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ResultadoResumenVehiculo> ObtenerResumenAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoResumenVehiculo.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        if (vehiculo is null)
            return ResultadoResumenVehiculo.CrearNoEncontrado("No se encontró el vehículo.");

        var (totalCargas, ultimaCarga) = await _recordatorioRepository.ObtenerResumenCargasAsync(vehiculoId, cancellationToken);
        var (totalServicios, ultimoServicio) = await _recordatorioRepository.ObtenerResumenServiciosAsync(vehiculoId, cancellationToken);
        var (totalRecordatorios, proximoRecordatorio) = await _recordatorioRepository.ObtenerResumenRecordatoriosAsync(vehiculoId, cancellationToken);

        var respuesta = new VehiculoResumenResponse
        {
            Id = vehiculo.Id,
            Marca = vehiculo.Marca,
            Modelo = vehiculo.Modelo,
            Anio = vehiculo.Anio,
            Placas = vehiculo.Placas,
            Color = vehiculo.Color,
            OdometroActual = vehiculo.OdometroActual,
            EsPrincipal = vehiculo.EsPrincipal,
            FotoUrl = vehiculo.FotoUrl,
            TotalCargas = totalCargas,
            UltimaCarga = ultimaCarga,
            TotalServicios = totalServicios,
            UltimoServicio = ultimoServicio,
            TotalRecordatoriosPendientes = totalRecordatorios,
            ProximoRecordatorio = proximoRecordatorio is null ? null : new ProximoRecordatorioDto
            {
                Id = proximoRecordatorio.Id,
                Titulo = proximoRecordatorio.Titulo,
                FechaLimite = proximoRecordatorio.FechaLimite,
                KmObjetivo = proximoRecordatorio.KmObjetivo,
                Criterio = proximoRecordatorio.Criterio
            }
        };

        return ResultadoResumenVehiculo.CrearExito(respuesta);
    }

    public async Task<ResultadoRecordatorio> CrearPorFechaAsync(
        Guid vehiculoId,
        CrearRecordatorioFechaRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new CrearRecordatorioFechaRequest();

        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoRecordatorio.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        if (vehiculo is null)
            return ResultadoRecordatorio.CrearNoEncontrado("No se encontró el vehículo.");

        var titulo = solicitud.Titulo?.Trim() ?? string.Empty;
        var errores = ValidarFecha(titulo, solicitud.FechaLimite, solicitud.AnticipacionDias);

        if (errores.Count > 0)
            return ResultadoRecordatorio.CrearValidacion(errores);

        var recordatorio = new Recordatorio
        {
            Id = Guid.NewGuid(),
            VehiculoId = vehiculoId,
            Tipo = NormalizarTipo(solicitud.Tipo, "otro"),
            Titulo = titulo,
            Criterio = "por_fecha",
            FechaLimite = solicitud.FechaLimite,
            KmObjetivo = null,
            AnticipacionDias = solicitud.AnticipacionDias is 3 or 7 or 15 or 30 ? solicitud.AnticipacionDias : (byte)7,
            PushPrioritaria = solicitud.PushPrioritaria,
            Estado = "pendiente",
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var guardado = await _recordatorioRepository.AgregarAsync(recordatorio, cancellationToken);

        return ResultadoRecordatorio.CrearExito(MapearRespuesta(guardado));
    }

    public async Task<ResultadoRecordatorio> CrearPorKilometrajeAsync(
        Guid vehiculoId,
        CrearRecordatorioKilometrajeRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        solicitud ??= new CrearRecordatorioKilometrajeRequest();

        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoRecordatorio.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        if (vehiculo is null)
            return ResultadoRecordatorio.CrearNoEncontrado("No se encontró el vehículo.");

        var titulo = solicitud.Titulo?.Trim() ?? string.Empty;
        var errores = ValidarKilometraje(titulo, solicitud.KmObjetivo, vehiculo.OdometroActual, solicitud.AnticipacionDias);

        if (errores.Count > 0)
            return ResultadoRecordatorio.CrearValidacion(errores);

        var recordatorio = new Recordatorio
        {
            Id = Guid.NewGuid(),
            VehiculoId = vehiculoId,
            Tipo = NormalizarTipo(solicitud.Tipo, "mantenimiento"),
            Titulo = titulo,
            Criterio = "por_kilometraje",
            FechaLimite = null,
            KmObjetivo = solicitud.KmObjetivo,
            AnticipacionDias = solicitud.AnticipacionDias is 3 or 7 or 15 or 30 ? solicitud.AnticipacionDias : (byte)7,
            PushPrioritaria = solicitud.PushPrioritaria,
            Estado = "pendiente",
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var guardado = await _recordatorioRepository.AgregarAsync(recordatorio, cancellationToken);

        return ResultadoRecordatorio.CrearExito(MapearRespuesta(guardado));
    }

    public async Task<ResultadoListaRecordatorios> ListarPorVehiculoAsync(
        Guid vehiculoId,
        string? estado = null,
        bool soloVencidos = false,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoListaRecordatorios.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var vehiculo = await _vehiculoRepository.ObtenerDetalleAsync(usuarioId.Value, vehiculoId, cancellationToken);

        if (vehiculo is null)
            return ResultadoListaRecordatorios.CrearNoEncontrado("No se encontró el vehículo.");

        var recordatorios = await _recordatorioRepository.ObtenerPorVehiculoAsync(vehiculoId, estado, cancellationToken);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var lista = recordatorios
            .Select(r => MapearDetalle(r, vehiculo.OdometroActual, hoy, vehiculo.Placas, $"{vehiculo.Marca} {vehiculo.Modelo}"))
            .Where(r => !soloVencidos || r.EstaVencido)
            .ToList();

        return ResultadoListaRecordatorios.CrearExito(lista);
    }

    public async Task<ResultadoListaRecordatorios> ListarPorUsuarioAsync(
        string? estado = "pendiente",
        bool soloVencidos = false,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoListaRecordatorios.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var recordatorios = await _recordatorioRepository.ObtenerPorUsuarioAsync(usuarioId.Value, estado, cancellationToken);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var lista = recordatorios
            .Select(r => MapearDetalle(
                r,
                r.Vehiculo?.OdometroActual ?? 0,
                hoy,
                r.Vehiculo?.Placas,
                r.Vehiculo != null ? $"{r.Vehiculo.Marca} {r.Vehiculo.Modelo}" : null))
            .Where(r => !soloVencidos || r.EstaVencido)
            .ToList();

        return ResultadoListaRecordatorios.CrearExito(lista);
    }

    public async Task<ResultadoRecordatorio> ConcluirAsync(
        Guid recordatorioId,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = _currentUserService.UsuarioId;

        if (usuarioId is null || usuarioId == Guid.Empty)
            return ResultadoRecordatorio.CrearNoAutorizado("Token inválido o usuario no identificado.");

        var recordatorio = await _recordatorioRepository.ObtenerPorIdAsync(recordatorioId, cancellationToken);

        if (recordatorio is null)
            return ResultadoRecordatorio.CrearNoEncontrado("No se encontró el recordatorio.");

        if (recordatorio.Vehiculo == null || recordatorio.Vehiculo.UsuarioId != usuarioId.Value)
            return ResultadoRecordatorio.CrearNoAutorizado("No tiene permisos para modificar este recordatorio.");

        recordatorio.Estado = "concluido";
        recordatorio.FechaConcluido = DateTime.UtcNow;

        await _recordatorioRepository.ActualizarAsync(recordatorio, cancellationToken);

        return ResultadoRecordatorio.CrearExito(MapearRespuesta(recordatorio));
    }

    private static RecordatorioDetalleResponse MapearDetalle(
        Recordatorio r,
        int odometroActual,
        DateOnly hoy,
        string? placas,
        string? modelo)
    {
        bool estaVencido = false;
        int? diasRestantes = null;
        int? kmRestantes = null;
        bool proximoAVencer = false;

        if (r.Criterio == "por_fecha" && r.FechaLimite.HasValue)
        {
            diasRestantes = r.FechaLimite.Value.DayNumber - hoy.DayNumber;
            estaVencido = diasRestantes < 0;
            proximoAVencer = !estaVencido && diasRestantes <= r.AnticipacionDias;
        }
        else if (r.Criterio == "por_kilometraje" && r.KmObjetivo.HasValue)
        {
            kmRestantes = r.KmObjetivo.Value - odometroActual;
            estaVencido = kmRestantes <= 0;
            proximoAVencer = !estaVencido && kmRestantes <= 500;
        }

        return new RecordatorioDetalleResponse
        {
            Id = r.Id,
            VehiculoId = r.VehiculoId,
            VehiculoPlacas = placas,
            VehiculoModelo = modelo,
            Tipo = r.Tipo,
            Titulo = r.Titulo,
            Criterio = r.Criterio,
            FechaLimite = r.FechaLimite,
            KmObjetivo = r.KmObjetivo,
            AnticipacionDias = r.AnticipacionDias,
            PushPrioritaria = r.PushPrioritaria,
            Estado = r.Estado,
            FechaConcluido = r.FechaConcluido,
            CreadoEn = r.CreadoEn,
            EstaVencido = estaVencido,
            DiasRestantes = diasRestantes,
            KmRestantes = kmRestantes,
            ProximoAVencer = proximoAVencer
        };
    }

    private static string NormalizarTipo(string? tipo, string valorPorDefecto)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return valorPorDefecto;

        var normalizado = tipo.Trim().ToLowerInvariant();
        return TiposValidos.Contains(normalizado) ? normalizado : valorPorDefecto;
    }

    private static RecordatorioResponse MapearRespuesta(Recordatorio r) => new()
    {
        Id = r.Id,
        VehiculoId = r.VehiculoId,
        Tipo = r.Tipo,
        Titulo = r.Titulo,
        Criterio = r.Criterio,
        FechaLimite = r.FechaLimite,
        KmObjetivo = r.KmObjetivo,
        AnticipacionDias = r.AnticipacionDias,
        PushPrioritaria = r.PushPrioritaria,
        Estado = r.Estado,
        CreadoEn = r.CreadoEn
    };

    private static List<string> ValidarFecha(string titulo, DateOnly fechaLimite, byte anticipacionDias)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(titulo))
            errores.Add("El título es obligatorio.");
        else if (titulo.Length > 120)
            errores.Add("El título no puede superar los 120 caracteres.");

        if (fechaLimite < DateOnly.FromDateTime(DateTime.UtcNow))
            errores.Add("La fecha límite no puede ser anterior a hoy.");

        if (anticipacionDias is not (3 or 7 or 15 or 30))
            errores.Add("Los días de anticipación permitidos son 3, 7, 15 o 30.");

        return errores;
    }

    private static List<string> ValidarKilometraje(string titulo, int kmObjetivo, int odometroActual, byte anticipacionDias)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(titulo))
            errores.Add("El título es obligatorio.");
        else if (titulo.Length > 120)
            errores.Add("El título no puede superar los 120 caracteres.");

        if (kmObjetivo <= 0)
            errores.Add("El km objetivo debe ser mayor a cero.");
        else if (kmObjetivo <= odometroActual)
            errores.Add($"El km objetivo debe ser mayor al odómetro actual ({odometroActual} km).");

        if (anticipacionDias is not (3 or 7 or 15 or 30))
            errores.Add("Los días de anticipación permitidos son 3, 7, 15 o 30.");

        return errores;
    }
}
