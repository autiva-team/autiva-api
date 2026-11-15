using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class CargaServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();

    private readonly Mock<ICargaRepository> _cargaRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private CargaService CrearServicio() => new(
        _cargaRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

    private static Vehiculo Vehiculo() => new() { Id = VehiculoId, UsuarioId = UsuarioId };

    private static CargaCombustible Carga(DateTime fechaHora, int odometro, decimal litros = 40m, decimal costoTotal = 880m) => new()
    {
        Id = Guid.NewGuid(),
        VehiculoId = VehiculoId,
        FechaHora = fechaHora,
        Odometro = odometro,
        Litros = litros,
        CostoTotal = costoTotal,
        TipoCombustible = new TipoCombustible { Id = 1, Nombre = "Magna" }
    };

    private static RegistrarCargaRequest SolicitudValida() => new()
    {
        TipoCombustibleId = 1,
        Litros = 40m,
        CostoTotal = 880m,
        Odometro = 1000,
        TanqueLleno = true,
        FechaHora = DateTime.UtcNow
    };

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

    private void PrepararUsuarioVehiculoYTipo(int odometroActual = 1000)
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Vehiculo { Id = VehiculoId, UsuarioId = UsuarioId, OdometroActual = odometroActual });
        _cargaRepository
            .Setup(r => r.ObtenerTipoCombustibleAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TipoCombustible { Id = 1, Nombre = "Magna" });
    }

    [Fact]
    public async Task RegistrarAsync_LitrosCero_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioVehiculoYTipo();
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Litros = 0m;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("Los litros deben ser mayores a cero.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_CostoTotalCero_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioVehiculoYTipo();
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.CostoTotal = 0m;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El costo total debe ser mayor a cero.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_OdometroMenorAlActual_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioVehiculoYTipo(odometroActual: 1000);
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Odometro = 500;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El odómetro no puede ser menor al actual (1000 km).", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_FechaFutura_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioVehiculoYTipo();
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.FechaHora = DateTime.UtcNow.AddHours(1);

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("La fecha y hora de la carga no puede estar en el futuro.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_TipoCombustibleInexistente_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Vehiculo { Id = VehiculoId, UsuarioId = UsuarioId, OdometroActual = 1000 });
        _cargaRepository
            .Setup(r => r.ObtenerTipoCombustibleAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TipoCombustible?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El tipo de combustible no existe.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_IdRepetido_RegresaConflicto()
    {
        // Arrange
        PrepararUsuarioVehiculoYTipo();
        _cargaRepository
            .Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Conflicto);
        Assert.Equal("La carga ya fue registrada.", resultado.Mensaje);
    }

    [Fact]
    public async Task RegistrarAsync_PrimeraCargaDelVehiculo_RegresaExitoSinRendimientoNiKm()
    {
        // Arrange: no hay ninguna carga anterior del vehículo.
        PrepararUsuarioVehiculoYTipo();
        _cargaRepository
            .Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _cargaRepository
            .Setup(r => r.ObtenerUltimaAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CargaCombustible?)null);
        _cargaRepository
            .Setup(r => r.RegistrarAsync(It.IsAny<CargaCombustible>(), VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CargaCombustible c, Guid _, CancellationToken _) => c);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.True(resultado.Exito);
        Assert.Null(resultado.Datos!.KmRecorridos);
        Assert.Null(resultado.Datos.RendimientoKml);
    }

    [Fact]
    public async Task RegistrarAsync_TanqueLlenoConAnterior_CalculaRendimiento()
    {
        // Arrange: carga anterior de tanque lleno en odómetro 600; la nueva llega en 1000
        // con 40 litros, sin cargas parciales entre medio -> rendimiento = 400/40 = 10.
        PrepararUsuarioVehiculoYTipo(odometroActual: 600);
        var anterior = Carga(DateTime.UtcNow.AddDays(-5), 600);
        _cargaRepository
            .Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _cargaRepository
            .Setup(r => r.ObtenerUltimaAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anterior);
        _cargaRepository
            .Setup(r => r.ObtenerUltimaTanqueLlenoAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anterior);
        _cargaRepository
            .Setup(r => r.SumarLitrosPosterioresAsync(VehiculoId, anterior.FechaHora, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _cargaRepository
            .Setup(r => r.RegistrarAsync(It.IsAny<CargaCombustible>(), VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CargaCombustible c, Guid _, CancellationToken _) => c);
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Odometro = 1000;
        solicitud.Litros = 40m;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(400, resultado.Datos!.KmRecorridos);
        Assert.Equal(10m, resultado.Datos.RendimientoKml);
    }

    // ---------- ObtenerHistorialAsync ----------

    [Fact]
    public async Task ObtenerHistorialAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerHistorialAsync(VehiculoId, 1, 10, null, null);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ObtenerHistorialAsync_VehiculoNoExiste_RegresaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerHistorialAsync(VehiculoId, 1, 10, null, null);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerHistorialAsync_DesdeMayorQueHasta_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        var servicio = CrearServicio();
        var desde = new DateTime(2026, 10, 20);
        var hasta = new DateTime(2026, 10, 10);

        // Act
        var resultado = await servicio.ObtenerHistorialAsync(VehiculoId, 1, 10, desde, hasta);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
    }

    [Fact]
    public async Task ObtenerHistorialAsync_ConCargas_RegresaListaYPaginacionCorrecta()
    {
        // Arrange: página 1 de tamaño 2 con 5 cargas en total; la más reciente de la
        // página (sin "siguiente" en la lista) necesita buscar su anterior en la base.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());

        var masReciente = Carga(new DateTime(2026, 11, 3), 12100);
        var siguiente = Carga(new DateTime(2026, 11, 1), 11700);
        var anteriorEnBase = Carga(new DateTime(2026, 10, 28), 11200);

        _cargaRepository
            .Setup(r => r.ListarHistorialAsync(VehiculoId, 1, 2, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<CargaCombustible> { masReciente, siguiente }, 5));
        _cargaRepository
            .Setup(r => r.ObtenerAnteriorAsync(VehiculoId, siguiente.FechaHora, siguiente.Odometro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anteriorEnBase);

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerHistorialAsync(VehiculoId, 1, 2, null, null);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(2, resultado.Datos!.Items.Count);
        Assert.Equal(5, resultado.Datos.TotalRegistros);
        Assert.Equal(3, resultado.Datos.TotalPaginas);
        Assert.Equal(400, resultado.Datos.Items[0].KmRecorridos); // 12100 - 11700
        Assert.Equal(500, resultado.Datos.Items[1].KmRecorridos); // 11700 - 11200
    }

    // ---------- ObtenerDetalleAsync ----------

    [Fact]
    public async Task ObtenerDetalleAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerDetalleAsync(Guid.NewGuid());

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_CargaNoExiste_RegresaNoEncontrado()
    {
        // Arrange: el repositorio ya filtra por usuario, así que "no existe", "está
        // eliminada" y "es de otro usuario" regresan el mismo null.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _cargaRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CargaCombustible?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerDetalleAsync(Guid.NewGuid());

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_CargaExiste_RegresaDatosConKmRecorridosCorrecto()
    {
        // Arrange
        var carga = Carga(new DateTime(2026, 11, 3), 12100, litros: 40m, costoTotal: 880m);
        var anterior = Carga(new DateTime(2026, 11, 1), 11700);

        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _cargaRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, carga.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(carga);
        _cargaRepository
            .Setup(r => r.ObtenerAnteriorAsync(VehiculoId, carga.FechaHora, carga.Odometro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anterior);

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerDetalleAsync(carga.Id);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(carga.Id, resultado.Datos!.Id);
        Assert.Equal(400, resultado.Datos.KmRecorridos);
        Assert.Equal(22m, resultado.Datos.PrecioPorLitro);
        Assert.Equal("Magna", resultado.Datos.TipoCombustible);
    }
}
