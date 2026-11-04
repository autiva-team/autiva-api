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
