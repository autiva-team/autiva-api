using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class VehiculoServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();

    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private VehiculoService CrearServicio() => new(
        _vehiculoRepository.Object,
        _currentUserService.Object);

    private static Vehiculo Vehiculo(bool esPrincipal = false, int odometroActual = 1000) => new()
    {
        Id = VehiculoId,
        UsuarioId = UsuarioId,
        Marca = "Nissan",
        Modelo = "Versa",
        Anio = 2020,
        Placas = "ABC123",
        OdometroActual = odometroActual,
        Estatus = "activo",
        EsPrincipal = esPrincipal
    };

    private static RegistrarVehiculoRequest SolicitudValida() => new()
    {
        Marca = "Nissan",
        Modelo = "Versa",
        Anio = 2020,
        Placas = "abc123",
        KilometrajeInicial = 1000
    };

    // ---------- RegistrarAsync ----------

    [Fact]
    public async Task RegistrarAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(SolicitudValida(), default);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task RegistrarAsync_MarcaVacia_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Marca = " ";

        // Act
        var resultado = await servicio.RegistrarAsync(solicitud, default);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("La marca es obligatoria.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_PlacasDuplicadas_RegresaConflicto()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ExistePlacasAsync(UsuarioId, "ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(SolicitudValida(), default);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Conflicto);
        Assert.Equal("Ya tienes un vehículo registrado con esas placas.", resultado.Mensaje);
    }

    [Fact]
    public async Task RegistrarAsync_ConflictoAlGuardar_RegresaConflicto()
    {
        // Arrange: pasó la validación de placas pero el guardado choca por una carrera
        // sobre el índice único (dos registros simultáneos con las mismas placas).
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ExistePlacasAsync(UsuarioId, "ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _vehiculoRepository
            .Setup(r => r.TieneVehiculosAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _vehiculoRepository
            .Setup(r => r.AgregarAsync(It.IsAny<Vehiculo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(SolicitudValida(), default);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Conflicto);
    }

    [Fact]
    public async Task RegistrarAsync_PrimerVehiculoDelUsuario_RegresaExitoComoPrincipal()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ExistePlacasAsync(UsuarioId, "ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _vehiculoRepository
            .Setup(r => r.TieneVehiculosAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _vehiculoRepository
            .Setup(r => r.AgregarAsync(It.IsAny<Vehiculo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(SolicitudValida(), default);

        // Assert
        Assert.True(resultado.Exito);
        Assert.True(resultado.Datos!.EsPrincipal);
        Assert.Equal("ABC123", resultado.Datos.Placas);
    }

    // ---------- ListarAsync ----------

    [Fact]
    public async Task ListarAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ListarAsync();

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ListarAsync_ConVehiculos_RegresaListaMapeada()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ListarAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vehiculo> { Vehiculo(esPrincipal: true) });
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ListarAsync();

        // Assert
        Assert.True(resultado.Exito);
        Assert.Single(resultado.Datos);
        Assert.Equal("ABC123", resultado.Datos[0].Placas);
    }

    // ---------- ObtenerDetalleAsync ----------

    [Fact]
    public async Task ObtenerDetalleAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerDetalleAsync(VehiculoId);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_VehiculoNoExiste_RegresaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerDetalleAsync(VehiculoId);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_VehiculoExiste_RegresaDatos()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo());
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerDetalleAsync(VehiculoId);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(VehiculoId, resultado.Datos!.Id);
    }

    // ---------- MarcarActivoAsync ----------

    [Fact]
    public async Task MarcarActivoAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.MarcarActivoAsync(VehiculoId);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task MarcarActivoAsync_VehiculoNoExiste_RegresaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.MarcarActivoAsync(VehiculoId);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task MarcarActivoAsync_YaEsPrincipal_RegresaExitoSinActualizar()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(esPrincipal: true));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.MarcarActivoAsync(VehiculoId);

        // Assert
        Assert.True(resultado.Exito);
        _vehiculoRepository.Verify(
            r => r.MarcarActivoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MarcarActivoAsync_NoEsPrincipal_LoMarcaYRegresaExito()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(esPrincipal: false));
        _vehiculoRepository
            .Setup(r => r.MarcarActivoAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(esPrincipal: true));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.MarcarActivoAsync(VehiculoId);

        // Assert
        Assert.True(resultado.Exito);
        Assert.True(resultado.Datos!.EsPrincipal);
    }

    // ---------- ActualizarOdometroAsync ----------

    [Fact]
    public async Task ActualizarOdometroAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ActualizarOdometroAsync(VehiculoId, new ActualizarOdometroRequest { OdometroActual = 1500 });

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ActualizarOdometroAsync_VehiculoNoExiste_RegresaNoEncontrado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ActualizarOdometroAsync(VehiculoId, new ActualizarOdometroRequest { OdometroActual = 1500 });

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ActualizarOdometroAsync_OdometroNegativo_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(odometroActual: 1000));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ActualizarOdometroAsync(VehiculoId, new ActualizarOdometroRequest { OdometroActual = -1 });

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("El odómetro no puede ser negativo.", resultado.Mensaje);
    }

    [Fact]
    public async Task ActualizarOdometroAsync_OdometroMenorAlActual_RegresaValidacion()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(odometroActual: 1000));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ActualizarOdometroAsync(VehiculoId, new ActualizarOdometroRequest { OdometroActual = 500 });

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("El odómetro no puede ser menor al actual (1000 km).", resultado.Mensaje);
    }

    [Fact]
    public async Task ActualizarOdometroAsync_IgualAlActual_RegresaExitoSinActualizar()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(odometroActual: 1000));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ActualizarOdometroAsync(VehiculoId, new ActualizarOdometroRequest { OdometroActual = 1000 });

        // Assert
        Assert.True(resultado.Exito);
        _vehiculoRepository.Verify(
            r => r.ActualizarOdometroAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ActualizarOdometroAsync_MayorAlActual_ActualizaYRegresaExito()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(odometroActual: 1000));
        _vehiculoRepository
            .Setup(r => r.ActualizarOdometroAsync(UsuarioId, VehiculoId, 1500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(odometroActual: 1500));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ActualizarOdometroAsync(VehiculoId, new ActualizarOdometroRequest { OdometroActual = 1500 });

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(1500, resultado.Datos!.OdometroActual);
    }
}
