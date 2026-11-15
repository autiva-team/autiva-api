using Autiva.Application.Dtos;
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

    private static Vehiculo Vehiculo(int odometroActual = 0) => new() { Id = VehiculoId, UsuarioId = UsuarioId, OdometroActual = odometroActual };

    private void ConfigurarGastosDelRepositorio(params VwGastosConsolidados[] gastos)
    {
        _gastoRepository
            .Setup(r => r.ListarPorVehiculoYRangoAsync(VehiculoId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gastos);
    }

    private static CategoriaGasto Categoria(byte id = 1, string nombre = "Lavado") => new() { Id = id, Nombre = nombre };

    private static RegistrarGastoRequest SolicitudValida() => new()
    {
        CategoriaId = 1,
        Concepto = "Lavado exterior",
        Monto = 150m,
        Fecha = null,
        Odometro = null
    };

    private static Gasto GastoRegistrado(DateOnly fecha, string categoriaNombre, decimal monto) => new()
    {
        Id = Guid.NewGuid(),
        VehiculoId = VehiculoId,
        CategoriaId = 1,
        Concepto = categoriaNombre,
        Monto = monto,
        Fecha = fecha,
        Categoria = new CategoriaGasto { Id = 1, Nombre = categoriaNombre }
    };

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

    // ---------- RegistrarAsync ----------

    [Fact]
    public async Task RegistrarAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task RegistrarAsync_VehiculoNoExiste_RegresaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task RegistrarAsync_CategoriaNoExiste_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoriaGasto?)null);
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.CategoriaId = 99;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("La categoría no existe.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_ConceptoVacio_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Concepto = "   ";

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El concepto es obligatorio.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_ConceptoMuyLargo_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Concepto = new string('a', 121);

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El concepto no puede superar los 120 caracteres.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_MontoCero_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Monto = 0m;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El monto debe ser mayor a cero.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_FechaFutura_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Fecha = DateOnly.FromDateTime(HoyMexico()).AddDays(1);

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("La fecha del gasto no puede estar en el futuro.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_OdometroNegativo_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Odometro = -1;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El odómetro no puede ser negativo.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_IdRepetido_RegresaConflicto()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        _gastoRepository
            .Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Conflicto);
    }

    [Fact]
    public async Task RegistrarAsync_DatosValidos_RegistraYRegresaExito()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(odometroActual: 1000));
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        _gastoRepository
            .Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _gastoRepository
            .Setup(r => r.RegistrarAsync(It.IsAny<Gasto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Gasto g, CancellationToken _) => g);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal("Lavado exterior", resultado.Datos!.Concepto);
        Assert.Equal("Lavado", resultado.Datos.Categoria);
        Assert.Equal(150m, resultado.Datos.Monto);
        Assert.Equal(DateOnly.FromDateTime(HoyMexico()), resultado.Datos.Fecha);
        _vehiculoRepository.Verify(
            r => r.ActualizarOdometroAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrarAsync_OdometroMayorAlActual_ActualizaOdometro()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(odometroActual: 1000));
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        _gastoRepository
            .Setup(r => r.RegistrarAsync(It.IsAny<Gasto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Gasto g, CancellationToken _) => g);
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Odometro = 1200;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.True(resultado.Exito);
        _vehiculoRepository.Verify(
            r => r.ActualizarOdometroAsync(UsuarioId, VehiculoId, 1200, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_OdometroMenorAlActual_NoActualizaOdometro()
    {
        // Arrange: puede ser un gasto viejo, se acepta sin tocar el odómetro.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(odometroActual: 1000));
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        _gastoRepository
            .Setup(r => r.RegistrarAsync(It.IsAny<Gasto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Gasto g, CancellationToken _) => g);
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Odometro = 500;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.True(resultado.Exito);
        _vehiculoRepository.Verify(
            r => r.ActualizarOdometroAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- ListarAsync ----------

    [Fact]
    public async Task ListarAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ListarAsync(VehiculoId, null, null, null, 1, 10);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ListarAsync_VehiculoNoExiste_RegresaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ListarAsync(VehiculoId, null, null, null, 1, 10);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ListarAsync_SoloAnioSinMes_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ListarAsync(VehiculoId, 2026, null, null, 1, 10);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
    }

    [Fact]
    public async Task ListarAsync_CategoriaNoExiste_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoriaGasto?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ListarAsync(VehiculoId, null, null, categoriaId: 99, pagina: 1, tamano: 10);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
    }

    [Fact]
    public async Task ListarAsync_ConFiltros_RegresaListaPaginada()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        _gastoRepository
            .Setup(r => r.ObtenerCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Categoria());
        var hoy = PrimerDiaDelMes(0);
        var gastos = new List<Gasto>
        {
            GastoRegistrado(hoy, "Lavado", 150m)
        };
        _gastoRepository
            .Setup(r => r.ListarHistorialAsync(VehiculoId, 1, 10, hoy.Year, hoy.Month, (byte)1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((gastos, 1));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ListarAsync(VehiculoId, hoy.Year, hoy.Month, categoriaId: 1, pagina: 1, tamano: 10);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Single(resultado.Datos!.Items);
        Assert.Equal("Lavado", resultado.Datos.Items[0].Categoria);
        Assert.Equal(1, resultado.Datos.TotalRegistros);
        Assert.Equal(1, resultado.Datos.TotalPaginas);
    }
}
