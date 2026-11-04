using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class RendimientoServiceTests
{
    private static readonly TimeZoneInfo ZonaMexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();

    private readonly Mock<ICargaRepository> _cargaRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private RendimientoService CrearServicio() => new(
        _cargaRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

    /// <summary>Fecha en UTC que cae en el día <paramref name="dia"/> a las <paramref name="hora"/>
    /// horas, hora de México, del mes actual + <paramref name="offsetMeses"/> meses. Se calcula a
    /// partir de "ahora" para que la prueba no dependa de en qué fecha se ejecute.</summary>
    private static DateTime FechaUtcEnMexico(int offsetMeses, int dia, int hora)
    {
        var ahoraMexico = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaMexico);
        var fechaMexico = new DateTime(ahoraMexico.Year, ahoraMexico.Month, 1, hora, 0, 0).AddMonths(offsetMeses);
        fechaMexico = fechaMexico.AddDays(dia - 1);
        return TimeZoneInfo.ConvertTimeToUtc(fechaMexico, ZonaMexico);
    }

    private static CargaCombustible Carga(DateTime fechaHoraUtc, decimal costoTotal, decimal? rendimientoKml) => new()
    {
        Id = Guid.NewGuid(),
        VehiculoId = VehiculoId,
        FechaHora = fechaHoraUtc,
        CostoTotal = costoTotal,
        RendimientoKml = rendimientoKml
    };

    private static Vehiculo VehiculoConCapacidad(decimal? capacidadTanqueL) => new()
    {
        Id = VehiculoId,
        UsuarioId = UsuarioId,
        CapacidadTanqueL = capacidadTanqueL
    };

    [Fact]
    public async Task ObtenerAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ObtenerAsync_VehiculoNoExiste_RegresaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerAsync_VehiculoEsDeOtroUsuario_RegresaNoEncontrado()
    {
        // Arrange: el repositorio filtra por usuario, así que para el servicio es
        // indistinguible de "no existe" (mismo null, mismo mensaje).
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
        _vehiculoRepository.Verify(
            r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObtenerAsync_VehiculoSinCargas_RegresaValoresNulosYTotalCero()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoConCapacidad(45m));
        _cargaRepository
            .Setup(r => r.ListarNoEliminadasAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CargaCombustible>());
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Null(resultado.Datos!.RendimientoPromedioKml);
        Assert.Null(resultado.Datos.VariacionMesAnteriorPorcentaje);
        Assert.Null(resultado.Datos.AutonomiaEstimadaKm);
        Assert.Equal(0m, resultado.Datos.GastoMesActual);
        Assert.Equal(0, resultado.Datos.TotalCargas);
    }

    [Fact]
    public async Task ObtenerAsync_CargasSinNingunRendimientoPrevio_PromedioYAutonomiaSonNull()
    {
        // Arrange: el vehículo tiene cargas, pero ninguna con rendimiento_kml
        // calculado (p. ej. todas parciales, sin un tanque lleno anterior).
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoConCapacidad(45m));
        _cargaRepository
            .Setup(r => r.ListarNoEliminadasAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CargaCombustible>
            {
                Carga(FechaUtcEnMexico(0, 1, 12), costoTotal: 500m, rendimientoKml: null)
            });
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Null(resultado.Datos!.RendimientoPromedioKml);
        Assert.Null(resultado.Datos.VariacionMesAnteriorPorcentaje);
        Assert.Null(resultado.Datos.AutonomiaEstimadaKm);
        Assert.Equal(1, resultado.Datos.TotalCargas);
    }

    [Fact]
    public async Task ObtenerAsync_ConCargasDeDosMeses_CalculaPromedioVariacionYGastoCorrectos()
    {
        // Arrange: 2 cargas el mes anterior (rendimiento 10 y 8, promedio 9) y
        // 2 cargas este mes (rendimiento 10 y 10, promedio 10); misma cuenta que
        // se verificó a mano contra la API real.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoConCapacidad(45m));
        _cargaRepository
            .Setup(r => r.ListarNoEliminadasAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CargaCombustible>
            {
                Carga(FechaUtcEnMexico(-1, 1, 12), costoTotal: 900m, rendimientoKml: 10m),
                Carga(FechaUtcEnMexico(-1, 2, 12), costoTotal: 1150m, rendimientoKml: 8m),
                Carga(FechaUtcEnMexico(0, 1, 12), costoTotal: 1200m, rendimientoKml: 10m),
                Carga(FechaUtcEnMexico(0, 2, 12), costoTotal: 880m, rendimientoKml: 10m)
            });
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync(VehiculoId);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(9.5m, resultado.Datos!.RendimientoPromedioKml);
        Assert.Equal(11.11m, resultado.Datos.VariacionMesAnteriorPorcentaje);
        Assert.Equal(2080m, resultado.Datos.GastoMesActual);
        Assert.Equal(427.5m, resultado.Datos.AutonomiaEstimadaKm);
        Assert.Equal(4, resultado.Datos.TotalCargas);
    }
}
