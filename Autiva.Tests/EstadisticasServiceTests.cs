using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class EstadisticasServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(ZonaMexico.Ahora());
    private static readonly DateOnly DesdeUnMes = Hoy.AddMonths(-1);

    private readonly Mock<IGastoRepository> _gastoRepository = new();
    private readonly Mock<ICargaRepository> _cargaRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    public EstadisticasServiceTests()
    {
        // Por defecto, ningún rango de fechas devuelve registros; las pruebas que
        // necesitan datos sobreescriben solo el rango exacto que les interesa.
        _gastoRepository
            .Setup(r => r.ListarPorVehiculoYRangoAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<VwGastosConsolidados>());
        _cargaRepository
            .Setup(r => r.ListarNoEliminadasAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CargaCombustible>());
    }

    private EstadisticasService CrearServicio() => new(
        _gastoRepository.Object,
        _cargaRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

    private void PrepararUsuarioYVehiculo()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Vehiculo { Id = VehiculoId, UsuarioId = UsuarioId, Marca = "Nissan", Modelo = "Versa", OdometroActual = 20000 });
    }

    private static VwGastosConsolidados GastoConsolidado(decimal? monto, int? odometro) => new()
    {
        VehiculoId = VehiculoId,
        Categoria = "combustible",
        Monto = monto,
        Odometro = odometro,
        Origen = "carga",
        OrigenId = Guid.NewGuid()
    };

    [Fact]
    public async Task ObtenerAsync_SinUsuarioAutenticado_RetornaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "1M");

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ObtenerAsync_VehiculoNoExiste_RetornaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "1M");

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerAsync_PeriodoInvalido_RetornaValidacion()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "2X");

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
        Assert.Equal("El periodo debe ser 1M, 3M, 6M o 1A.", resultado.Mensaje);
    }

    [Fact]
    public async Task ObtenerAsync_SinParametroDePeriodo_UsaUnMesPorDefecto()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, null);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal("1M", resultado.Datos!.Periodo);
        Assert.Equal(DesdeUnMes, resultado.Datos.Desde);
        Assert.Equal(Hoy, resultado.Datos.Hasta);
    }

    [Fact]
    public async Task ObtenerAsync_SinRegistros_RetornaCerosYNulls()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "1M");

        // Assert
        Assert.True(resultado.Exito);
        var datos = resultado.Datos!;
        Assert.Equal(0, datos.GastoTotal);
        Assert.Null(datos.DistanciaKm);
        Assert.Null(datos.CostoPorKm);
        Assert.Null(datos.VariacionCostoPorKm);
        Assert.Null(datos.RendimientoPromedio);
        Assert.Equal(0, datos.GastoProyectadoMes);
        Assert.Null(datos.DistanciaUltimos30Dias);

        var mesesEsperados = CalculadoraEstadisticas.ListarMesesDelPeriodo(DesdeUnMes, Hoy);
        Assert.Equal(mesesEsperados.Count, datos.RendimientoMensual.Count);
        Assert.All(datos.RendimientoMensual, m => Assert.Null(m.KmL));
    }

    [Fact]
    public async Task ObtenerAsync_ConGastosYOdometrosDentroDelPeriodoSinRegistrosAntes_CalculaGastoTotalYDistancia()
    {
        // Arrange: sin registros antes del periodo, la distancia resta el más bajo dentro.
        PrepararUsuarioYVehiculo();
        _gastoRepository
            .Setup(r => r.ListarPorVehiculoYRangoAsync(VehiculoId, DesdeUnMes, Hoy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { GastoConsolidado(600m, 10000), GastoConsolidado(400m, 10300) });
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "1M");

        // Assert
        var datos = resultado.Datos!;
        Assert.Equal(1000m, datos.GastoTotal);
        Assert.Equal(300, datos.DistanciaKm);
        Assert.Equal(3.33m, datos.CostoPorKm);
    }

    [Fact]
    public async Task ObtenerAsync_ConRegistrosAntesDelPeriodo_RestaElMaximoOdometroAntes()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        _gastoRepository
            .Setup(r => r.ListarPorVehiculoYRangoAsync(VehiculoId, DesdeUnMes, Hoy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { GastoConsolidado(500m, 10500) });
        _gastoRepository
            .Setup(r => r.ListarPorVehiculoYRangoAsync(VehiculoId, DateOnly.MinValue, DesdeUnMes.AddDays(-1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { GastoConsolidado(null, 10000) });
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "1M");

        // Assert
        Assert.Equal(500, resultado.Datos!.DistanciaKm);
    }

    [Fact]
    public async Task ObtenerAsync_RendimientoDeCargasDelPeriodo_PromedioPorMesYGeneral()
    {
        // Arrange: dos cargas de hoy con rendimiento 10 y 12 km/L -> promedio 11.
        PrepararUsuarioYVehiculo();

        // Mediodía en México, convertido a UTC, para caer siempre en la fecha "Hoy" sin
        // importar la hora UTC real en la que se ejecute la prueba.
        var fechaHoraUtc = DateTime.SpecifyKind(Hoy.ToDateTime(new TimeOnly(18, 0)), DateTimeKind.Utc);
        _cargaRepository
            .Setup(r => r.ListarNoEliminadasAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CargaCombustible { Id = Guid.NewGuid(), VehiculoId = VehiculoId, FechaHora = fechaHoraUtc, RendimientoKml = 10m },
                new CargaCombustible { Id = Guid.NewGuid(), VehiculoId = VehiculoId, FechaHora = fechaHoraUtc, RendimientoKml = 12m }
            });
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "1M");

        // Assert
        var datos = resultado.Datos!;
        Assert.Equal(11m, datos.RendimientoPromedio);
        var mesHoy = datos.RendimientoMensual.Single(m => m.Anio == Hoy.Year && m.Mes == Hoy.Month);
        Assert.Equal(11m, mesHoy.KmL);
    }

    [Fact]
    public async Task ObtenerAsync_GastoDelMesActual_ProyectaGastoDelMesCompleto()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        var primerDiaMesActual = new DateOnly(Hoy.Year, Hoy.Month, 1);
        _gastoRepository
            .Setup(r => r.ListarPorVehiculoYRangoAsync(VehiculoId, primerDiaMesActual, Hoy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { GastoConsolidado(600m, null) });
        var servicio = CrearServicio();
        var diasDelMes = DateTime.DaysInMonth(Hoy.Year, Hoy.Month);
        var esperado = Math.Round(600m / Hoy.Day * diasDelMes, 2);

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "1M");

        // Assert
        Assert.Equal(esperado, resultado.Datos!.GastoProyectadoMes);
    }

    [Fact]
    public async Task ObtenerAsync_DistanciaUltimos30Dias_UsaVentanaFijaIndependienteDelPeriodo()
    {
        // Arrange: ventana de 30 días con su propio rango, distinto al periodo de 1 año.
        PrepararUsuarioYVehiculo();
        var desde30 = Hoy.AddDays(-29);
        _gastoRepository
            .Setup(r => r.ListarPorVehiculoYRangoAsync(VehiculoId, desde30, Hoy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { GastoConsolidado(null, 5000), GastoConsolidado(null, 5200) });
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId, "1A");

        // Assert
        Assert.Equal(200, resultado.Datos!.DistanciaUltimos30Dias);
    }
}
