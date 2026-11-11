using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class RecordatorioServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly DateOnly Futuro = Hoy.AddDays(10);
    private static readonly DateOnly Pasado = Hoy.AddDays(-2);

    private readonly Mock<IRecordatorioRepository> _recordatorioRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private RecordatorioService CrearServicio() => new(
        _recordatorioRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

    private static Vehiculo VehiculoPrueba(int odometroActual = 15000) => new()
    {
        Id = VehiculoId,
        UsuarioId = UsuarioId,
        Marca = "Nissan",
        Modelo = "Versa",
        Anio = 2020,
        Placas = "ABC-123",
        Color = "Gris",
        OdometroActual = odometroActual,
        EsPrincipal = true,
        FotoUrl = null
    };

    private void PrepararUsuarioYVehiculo(int odometroActual = 15000)
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoPrueba(odometroActual));
    }

    // =========================================================================
    // HU-013: Resumen general del vehículo
    // =========================================================================

    [Fact]
    public async Task ObtenerResumen_SinUsuarioAutenticado_RetornaNoAutorizado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);

        var servicio = CrearServicio();
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId);

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
        Assert.Equal("Token inválido o usuario no identificado.", resultado.Mensaje);
    }

    [Fact]
    public async Task ObtenerResumen_VehiculoNoExisteONoPertenece_RetornaNoEncontrado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);

        var servicio = CrearServicio();
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId);

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
        Assert.Equal("No se encontró el vehículo.", resultado.Mensaje);
    }

    [Fact]
    public async Task ObtenerResumen_VehiculoExiste_RetornaResumenCompleto()
    {
        PrepararUsuarioYVehiculo(20000);

        var ultimaCargaFecha = DateTime.UtcNow.AddDays(-3);
        var ultimoServicioFecha = Hoy.AddDays(-15);
        var proximoRecordatorio = new Recordatorio
        {
            Id = Guid.NewGuid(),
            VehiculoId = VehiculoId,
            Titulo = "Verificación 1er Semestre",
            FechaLimite = Futuro,
            KmObjetivo = null,
            Criterio = "por_fecha"
        };

        _recordatorioRepository
            .Setup(r => r.ObtenerResumenCargasAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, ultimaCargaFecha));

        _recordatorioRepository
            .Setup(r => r.ObtenerResumenServiciosAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((2, ultimoServicioFecha));

        _recordatorioRepository
            .Setup(r => r.ObtenerResumenRecordatoriosAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, proximoRecordatorio));

        var servicio = CrearServicio();
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal(VehiculoId, resultado.Datos.Id);
        Assert.Equal("Nissan", resultado.Datos.Marca);
        Assert.Equal(20000, resultado.Datos.OdometroActual);
        Assert.Equal(5, resultado.Datos.TotalCargas);
        Assert.Equal(ultimaCargaFecha, resultado.Datos.UltimaCarga);
        Assert.Equal(2, resultado.Datos.TotalServicios);
        Assert.Equal(ultimoServicioFecha, resultado.Datos.UltimoServicio);
        Assert.Equal(3, resultado.Datos.TotalRecordatoriosPendientes);
        Assert.NotNull(resultado.Datos.ProximoRecordatorio);
        Assert.Equal(proximoRecordatorio.Id, resultado.Datos.ProximoRecordatorio.Id);
        Assert.Equal("Verificación 1er Semestre", resultado.Datos.ProximoRecordatorio.Titulo);
        Assert.Equal(Futuro, resultado.Datos.ProximoRecordatorio.FechaLimite);
    }

    [Fact]
    public async Task ObtenerResumen_VehiculoSinRegistros_RetornaContadoresEnCero()
    {
        PrepararUsuarioYVehiculo(5000);

        _recordatorioRepository
            .Setup(r => r.ObtenerResumenCargasAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, (DateTime?)null));

        _recordatorioRepository
            .Setup(r => r.ObtenerResumenServiciosAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, (DateOnly?)null));

        _recordatorioRepository
            .Setup(r => r.ObtenerResumenRecordatoriosAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, (Recordatorio?)null));

        var servicio = CrearServicio();
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal(0, resultado.Datos.TotalCargas);
        Assert.Null(resultado.Datos.UltimaCarga);
        Assert.Equal(0, resultado.Datos.TotalServicios);
        Assert.Null(resultado.Datos.UltimoServicio);
        Assert.Equal(0, resultado.Datos.TotalRecordatoriosPendientes);
        Assert.Null(resultado.Datos.ProximoRecordatorio);
    }

    // =========================================================================
    // HU-014: Crear recordatorio por fecha
    // =========================================================================

    [Fact]
    public async Task CrearPorFecha_SinUsuarioAutenticado_RetornaNoAutorizado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorFechaAsync(VehiculoId, new CrearRecordatorioFechaRequest());

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task CrearPorFecha_VehiculoNoExiste_RetornaNoEncontrado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorFechaAsync(VehiculoId, new CrearRecordatorioFechaRequest());

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CrearPorFecha_TituloVacio_RetornaErrorValidacion(string? tituloInvalido)
    {
        PrepararUsuarioYVehiculo();

        var solicitud = new CrearRecordatorioFechaRequest
        {
            Titulo = tituloInvalido!,
            FechaLimite = Futuro,
            AnticipacionDias = 7
        };

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorFechaAsync(VehiculoId, solicitud);

        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("título es obligatorio"));
    }

    [Fact]
    public async Task CrearPorFecha_TituloMayorA120_RetornaErrorValidacion()
    {
        PrepararUsuarioYVehiculo();

        var solicitud = new CrearRecordatorioFechaRequest
        {
            Titulo = new string('A', 121),
            FechaLimite = Futuro,
            AnticipacionDias = 7
        };

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorFechaAsync(VehiculoId, solicitud);

        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("no puede superar los 120 caracteres"));
    }

    [Fact]
    public async Task CrearPorFecha_FechaLimitePasada_RetornaErrorValidacion()
    {
        PrepararUsuarioYVehiculo();

        var solicitud = new CrearRecordatorioFechaRequest
        {
            Titulo = "Pago de tenencia",
            FechaLimite = Pasado,
            AnticipacionDias = 7
        };

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorFechaAsync(VehiculoId, solicitud);

        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("no puede ser anterior a hoy"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(60)]
    public async Task CrearPorFecha_AnticipacionInvalida_RetornaErrorValidacion(byte diasInvalidos)
    {
        PrepararUsuarioYVehiculo();

        var solicitud = new CrearRecordatorioFechaRequest
        {
            Titulo = "Renovación seguro",
            FechaLimite = Futuro,
            AnticipacionDias = diasInvalidos
        };

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorFechaAsync(VehiculoId, solicitud);

        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("3, 7, 15 o 30"));
    }

    [Fact]
    public async Task CrearPorFecha_DatosValidos_GuardaYRetornaRecordatorio()
    {
        PrepararUsuarioYVehiculo();

        var solicitud = new CrearRecordatorioFechaRequest
        {
            Titulo = "Póliza de seguro",
            Tipo = "seguro",
            FechaLimite = Futuro,
            AnticipacionDias = 15,
            PushPrioritaria = true
        };

        Recordatorio? guardado = null;
        _recordatorioRepository
            .Setup(r => r.AgregarAsync(It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()))
            .Callback<Recordatorio, CancellationToken>((r, _) => guardado = r)
            .ReturnsAsync((Recordatorio r, CancellationToken _) => r);

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorFechaAsync(VehiculoId, solicitud);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.NotNull(guardado);
        Assert.Equal("Póliza de seguro", guardado.Titulo);
        Assert.Equal("seguro", guardado.Tipo);
        Assert.Equal("por_fecha", guardado.Criterio);
        Assert.Equal(Futuro, guardado.FechaLimite);
        Assert.Null(guardado.KmObjetivo);
        Assert.Equal(15, guardado.AnticipacionDias);
        Assert.True(guardado.PushPrioritaria);
        Assert.Equal("pendiente", guardado.Estado);
        Assert.Equal(VehiculoId, guardado.VehiculoId);
    }

    [Fact]
    public async Task CrearPorFecha_TipoInvalido_UsaTipoPorDefectoOtro()
    {
        PrepararUsuarioYVehiculo();

        var solicitud = new CrearRecordatorioFechaRequest
        {
            Titulo = "Cita particular",
            Tipo = "tipo_inexistente",
            FechaLimite = Futuro,
            AnticipacionDias = 7
        };

        Recordatorio? guardado = null;
        _recordatorioRepository
            .Setup(r => r.AgregarAsync(It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()))
            .Callback<Recordatorio, CancellationToken>((r, _) => guardado = r)
            .ReturnsAsync((Recordatorio r, CancellationToken _) => r);

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorFechaAsync(VehiculoId, solicitud);

        Assert.True(resultado.Exito);
        Assert.NotNull(guardado);
        Assert.Equal("otro", guardado.Tipo);
    }

    // =========================================================================
    // HU-015: Crear recordatorio por kilometraje
    // =========================================================================

    [Fact]
    public async Task CrearPorKilometraje_SinUsuarioAutenticado_RetornaNoAutorizado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorKilometrajeAsync(VehiculoId, new CrearRecordatorioKilometrajeRequest());

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task CrearPorKilometraje_VehiculoNoExiste_RetornaNoEncontrado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorKilometrajeAsync(VehiculoId, new CrearRecordatorioKilometrajeRequest());

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CrearPorKilometraje_TituloVacio_RetornaErrorValidacion(string? tituloInvalido)
    {
        PrepararUsuarioYVehiculo(10000);

        var solicitud = new CrearRecordatorioKilometrajeRequest
        {
            Titulo = tituloInvalido!,
            KmObjetivo = 15000,
            AnticipacionDias = 7
        };

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorKilometrajeAsync(VehiculoId, solicitud);

        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("título es obligatorio"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task CrearPorKilometraje_KmObjetivoMenorOIgualACero_RetornaErrorValidacion(int kmInvalido)
    {
        PrepararUsuarioYVehiculo(0);

        var solicitud = new CrearRecordatorioKilometrajeRequest
        {
            Titulo = "Cambio de bujías",
            KmObjetivo = kmInvalido,
            AnticipacionDias = 7
        };

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorKilometrajeAsync(VehiculoId, solicitud);

        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("debe ser mayor a cero"));
    }

    [Theory]
    [InlineData(10000, 10000)]
    [InlineData(8000, 10000)]
    public async Task CrearPorKilometraje_KmObjetivoMenorOIgualAOdometro_RetornaErrorValidacion(int kmObjetivo, int odometroActual)
    {
        PrepararUsuarioYVehiculo(odometroActual);

        var solicitud = new CrearRecordatorioKilometrajeRequest
        {
            Titulo = "Cambio de frenos",
            KmObjetivo = kmObjetivo,
            AnticipacionDias = 7
        };

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorKilometrajeAsync(VehiculoId, solicitud);

        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains($"mayor al odómetro actual ({odometroActual} km)"));
    }

    [Fact]
    public async Task CrearPorKilometraje_DatosValidos_GuardaYRetornaRecordatorio()
    {
        PrepararUsuarioYVehiculo(12000);

        var solicitud = new CrearRecordatorioKilometrajeRequest
        {
            Titulo = "Cambio de aceite y filtro",
            Tipo = "mantenimiento",
            KmObjetivo = 17000,
            AnticipacionDias = 7,
            PushPrioritaria = false
        };

        Recordatorio? guardado = null;
        _recordatorioRepository
            .Setup(r => r.AgregarAsync(It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()))
            .Callback<Recordatorio, CancellationToken>((r, _) => guardado = r)
            .ReturnsAsync((Recordatorio r, CancellationToken _) => r);

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorKilometrajeAsync(VehiculoId, solicitud);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.NotNull(guardado);
        Assert.Equal("Cambio de aceite y filtro", guardado.Titulo);
        Assert.Equal("mantenimiento", guardado.Tipo);
        Assert.Equal("por_kilometraje", guardado.Criterio);
        Assert.Null(guardado.FechaLimite);
        Assert.Equal(17000, guardado.KmObjetivo);
        Assert.Equal(7, guardado.AnticipacionDias);
        Assert.False(guardado.PushPrioritaria);
        Assert.Equal("pendiente", guardado.Estado);
        Assert.Equal(VehiculoId, guardado.VehiculoId);
    }

    [Fact]
    public async Task CrearPorKilometraje_TipoNulo_UsaTipoPorDefectoMantenimiento()
    {
        PrepararUsuarioYVehiculo(5000);

        var solicitud = new CrearRecordatorioKilometrajeRequest
        {
            Titulo = "Rotación de neumáticos",
            Tipo = null,
            KmObjetivo = 10000,
            AnticipacionDias = 30
        };

        Recordatorio? guardado = null;
        _recordatorioRepository
            .Setup(r => r.AgregarAsync(It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()))
            .Callback<Recordatorio, CancellationToken>((r, _) => guardado = r)
            .ReturnsAsync((Recordatorio r, CancellationToken _) => r);

        var servicio = CrearServicio();
        var resultado = await servicio.CrearPorKilometrajeAsync(VehiculoId, solicitud);

        Assert.True(resultado.Exito);
        Assert.NotNull(guardado);
        Assert.Equal("mantenimiento", guardado.Tipo);
    }
}
