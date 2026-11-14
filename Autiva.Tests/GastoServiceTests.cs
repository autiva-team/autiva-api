using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class GastoServiceTests
{
    private static readonly TimeZoneInfo ZonaMexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();

    private readonly Mock<IGastoRepository> _gastoRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private GastoService CrearServicio() => new(
        _gastoRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

    private static DateTime HoyMexico() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaMexico);

    /// <summary>Día 1 del mes actual + <paramref name="offsetMeses"/> meses, hora de México.</summary>
    private static DateOnly PrimerDiaDelMes(int offsetMeses)
    {
        var hoy = HoyMexico();
        return new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(offsetMeses);
    }

    private static VwGastosConsolidados Gasto(DateOnly fecha, string categoria, decimal monto) => new()
    {
        VehiculoId = VehiculoId,
        Categoria = categoria,
        Concepto = categoria,
        Monto = monto,
        Fecha = fecha,
        Origen = "gasto",
        OrigenId = Guid.NewGuid()
    };

    private static Vehiculo Vehiculo() => new() { Id = VehiculoId, UsuarioId = UsuarioId };

    private void ConfigurarGastosDelRepositorio(params VwGastosConsolidados[] gastos)
    {
        _gastoRepository
            .Setup(r => r.ListarPorVehiculoYRangoAsync(VehiculoId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gastos);
    }

    // ---------- Casos de validación ----------

    [Fact]
    public async Task ObtenerResumenAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, null, null);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ObtenerResumenAsync_VehiculoNoExiste_RegresaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, null, null);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerResumenAsync_SoloAnioSinMes_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, anio: 2026, mes: null);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
    }

    [Fact]
    public async Task ObtenerResumenAsync_MesFueraDeRango_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, anio: 2026, mes: 13);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
    }

    [Fact]
    public async Task ObtenerResumenAsync_AnioFueraDeRango_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, anio: 0, mes: 5);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
    }

    [Fact]
    public async Task ObtenerResumenAsync_MesFuturo_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        var servicio = CrearServicio();

        var mesFuturo = PrimerDiaDelMes(1);

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, mesFuturo.Year, mesFuturo.Month);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
    }

    // ---------- Casos correctos ----------

    [Fact]
    public async Task ObtenerResumenAsync_SinParametros_UsaMesActualYCalculaTotalesYDesglose()
    {
        // Arrange: mes actual con combustible (500) y mantenimiento (300); mes
        // anterior con 400 de combustible. Totales y variación verificados a mano.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        ConfigurarGastosDelRepositorio(
            Gasto(PrimerDiaDelMes(0), "combustible", 500m),
            Gasto(PrimerDiaDelMes(0), "mantenimiento", 300m),
            Gasto(PrimerDiaDelMes(-1), "combustible", 400m));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, null, null);

        // Assert
        Assert.True(resultado.Exito);
        var datos = resultado.Datos!;
        Assert.Equal(HoyMexico().Year, datos.Anio);
        Assert.Equal(HoyMexico().Month, datos.Mes);
        Assert.Equal(800m, datos.TotalMes);
        Assert.Equal(400m, datos.TotalMesAnterior);
        Assert.Equal(100m, datos.VariacionPorcentaje);
        Assert.Equal(Math.Round(800m / HoyMexico().Day, 2), datos.CostoPromedioDiario);
        Assert.Equal(2, datos.Desglose.Count);
        Assert.Equal("combustible", datos.Desglose[0].Categoria);
        Assert.Equal(500m, datos.Desglose[0].Monto);
        Assert.Equal(62.5m, datos.Desglose[0].Porcentaje);
        Assert.Equal("mantenimiento", datos.Desglose[1].Categoria);
        Assert.Equal(300m, datos.Desglose[1].Monto);
        Assert.Equal(37.5m, datos.Desglose[1].Porcentaje);
    }

    [Fact]
    public async Task ObtenerResumenAsync_MesPasadoSinGastosElMesAnterior_VariacionEsNullYPromedioUsaDiasCompletos()
    {
        // Arrange: un mes pasado explícito (no el inmediato anterior a hoy) con un
        // solo gasto y nada en el mes previo a ese: no hay base para la variación.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        var mesPasado = PrimerDiaDelMes(-2);
        ConfigurarGastosDelRepositorio(
            Gasto(mesPasado, "seguro", 300m));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, mesPasado.Year, mesPasado.Month);

        // Assert
        Assert.True(resultado.Exito);
        var datos = resultado.Datos!;
        Assert.Equal(300m, datos.TotalMes);
        Assert.Equal(0m, datos.TotalMesAnterior);
        Assert.Null(datos.VariacionPorcentaje);
        var diasDelMes = DateTime.DaysInMonth(mesPasado.Year, mesPasado.Month);
        Assert.Equal(Math.Round(300m / diasDelMes, 2), datos.CostoPromedioDiario);
        Assert.Single(datos.Desglose);
        Assert.Equal("seguro", datos.Desglose[0].Categoria);
        Assert.Equal(100m, datos.Desglose[0].Porcentaje);
    }

    [Fact]
    public async Task ObtenerResumenAsync_MesSinGastos_RegresaCerosYDesgloseVacio()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        ConfigurarGastosDelRepositorio();
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerResumenAsync(VehiculoId, null, null);

        // Assert
        Assert.True(resultado.Exito);
        var datos = resultado.Datos!;
        Assert.Equal(0m, datos.TotalMes);
        Assert.Equal(0m, datos.TotalMesAnterior);
        Assert.Null(datos.VariacionPorcentaje);
        Assert.Equal(0m, datos.CostoPromedioDiario);
        Assert.Empty(datos.Desglose);
    }
}
