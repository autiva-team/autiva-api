using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class ServicioServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly Mock<IServicioRepository> _servicioRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private ServicioService CrearServicio() => new(
        _servicioRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

    private static Vehiculo Vehiculo(int kilometrajeInicial = 1000, int odometroActual = 1000) => new()
    {
        Id = VehiculoId,
        UsuarioId = UsuarioId,
        KilometrajeInicial = kilometrajeInicial,
        OdometroActual = odometroActual
    };

    private static RegistrarServicioRequest SolicitudValida() => new()
    {
        TipoServicioId = 1,
        Fecha = Hoy,
        KilometrajeServicio = 1500,
        Costo = 800m,
        MetaProximoKm = 6500,
        FechaRetorno = Hoy
    };

    private void PrepararUsuarioYVehiculo(int kilometrajeInicial = 1000, int odometroActual = 1000)
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Vehiculo(kilometrajeInicial, odometroActual));
    }

    private static ServicioMantenimiento Servicio(DateOnly fecha, int kilometraje) => new()
    {
        Id = Guid.NewGuid(),
        VehiculoId = VehiculoId,
        Fecha = fecha,
        KilometrajeServicio = kilometraje,
        Costo = 500m,
        TipoServicio = new TipoServicio { Id = 1, Nombre = "Aceite y filtros" }
    };

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
    public async Task RegistrarAsync_TipoServicioInexistente_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        _servicioRepository
            .Setup(r => r.ObtenerTipoServicioAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TipoServicio?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.False(resultado.Exito);
        Assert.False(resultado.NoEncontrado);
        Assert.False(resultado.Conflicto);
        Assert.Contains("El tipo de servicio no existe.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_CostoNegativo_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        _servicioRepository
            .Setup(r => r.ObtenerTipoServicioAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TipoServicio { Id = 1, Nombre = "Aceite y filtros" });
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Costo = -1m;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El costo no puede ser negativo.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_KilometrajeMenorAlInicialDelVehiculo_RegresaValidacion()
    {
        // Arrange: vehículo con kilometraje_inicial 1000; el servicio llega con 500.
        PrepararUsuarioYVehiculo(kilometrajeInicial: 1000, odometroActual: 1000);
        _servicioRepository
            .Setup(r => r.ObtenerTipoServicioAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TipoServicio { Id = 1, Nombre = "Aceite y filtros" });
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.KilometrajeServicio = 500;
        solicitud.MetaProximoKm = null;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("El kilometraje del servicio no puede ser menor al inicial del vehículo (1000 km).", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_MetaMenorOIgualAlKilometraje_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        _servicioRepository
            .Setup(r => r.ObtenerTipoServicioAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TipoServicio { Id = 1, Nombre = "Aceite y filtros" });
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.KilometrajeServicio = 1500;
        solicitud.MetaProximoKm = 1500; // igual, no mayor

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("La meta del próximo servicio debe ser mayor al kilometraje del servicio.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_FechaRetornoAnteriorAFecha_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        _servicioRepository
            .Setup(r => r.ObtenerTipoServicioAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TipoServicio { Id = 1, Nombre = "Aceite y filtros" });
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Fecha = Hoy;
        solicitud.FechaRetorno = Hoy.AddDays(-1);

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("La fecha de retorno no puede ser anterior a la fecha del servicio.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_FechaFutura_RegresaValidacion()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        _servicioRepository
            .Setup(r => r.ObtenerTipoServicioAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TipoServicio { Id = 1, Nombre = "Aceite y filtros" });
        var servicio = CrearServicio();
        var solicitud = SolicitudValida();
        solicitud.Fecha = Hoy.AddDays(1);
        solicitud.FechaRetorno = null;

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains("La fecha del servicio no puede estar en el futuro.", resultado.Errores);
    }

    [Fact]
    public async Task RegistrarAsync_IdRepetido_RegresaConflicto()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        _servicioRepository
            .Setup(r => r.ObtenerTipoServicioAsync(It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TipoServicio { Id = 1, Nombre = "Aceite y filtros" });
        _servicioRepository
            .Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Conflicto);
        Assert.Equal("El servicio ya fue registrado.", resultado.Mensaje);
    }

    [Fact]
    public async Task RegistrarAsync_DatosValidos_RegresaExitoConDatosCorrectos()
    {
        // Arrange
        PrepararUsuarioYVehiculo(kilometrajeInicial: 1000, odometroActual: 1000);
        _servicioRepository
            .Setup(r => r.ObtenerTipoServicioAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TipoServicio { Id = 1, Nombre = "Aceite y filtros" });
        _servicioRepository
            .Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _servicioRepository
            .Setup(r => r.RegistrarAsync(It.IsAny<ServicioMantenimiento>(), VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServicioMantenimiento s, Guid _, CancellationToken _) => s);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(VehiculoId, SolicitudValida());

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal("Aceite y filtros", resultado.Datos!.TipoServicio);
        Assert.Equal(1500, resultado.Datos.KilometrajeServicio);
        Assert.Equal(800m, resultado.Datos.Costo);
        Assert.Equal(6500, resultado.Datos.MetaProximoKm);
    }

    // ---------- ObtenerHistorialAsync ----------

    [Fact]
    public async Task ObtenerHistorialAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerHistorialAsync(VehiculoId, 1, 10);

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
        var resultado = await servicio.ObtenerHistorialAsync(VehiculoId, 1, 10);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerHistorialAsync_ConServicios_RegresaListaYPaginacionCorrecta()
    {
        // Arrange: 2 servicios en la página, de un total de 5, tamaño de página 2.
        PrepararUsuarioYVehiculo();
        var masReciente = Servicio(new DateOnly(2026, 11, 3), 12100);
        var siguiente = Servicio(new DateOnly(2026, 11, 1), 11700);

        _servicioRepository
            .Setup(r => r.ListarHistorialAsync(VehiculoId, 1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ServicioMantenimiento> { masReciente, siguiente }, 5));

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerHistorialAsync(VehiculoId, 1, 2);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(2, resultado.Datos!.Items.Count);
        Assert.Equal(5, resultado.Datos.TotalRegistros);
        Assert.Equal(3, resultado.Datos.TotalPaginas);
        Assert.Equal(masReciente.Id, resultado.Datos.Items[0].Id);
    }

    [Fact]
    public async Task ObtenerHistorialAsync_SinServicios_RegresaListaVacia()
    {
        // Arrange
        PrepararUsuarioYVehiculo();
        _servicioRepository
            .Setup(r => r.ListarHistorialAsync(VehiculoId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ServicioMantenimiento>(), 0));
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerHistorialAsync(VehiculoId, 1, 10);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Empty(resultado.Datos!.Items);
        Assert.Equal(0, resultado.Datos.TotalRegistros);
        Assert.Equal(0, resultado.Datos.TotalPaginas);
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
    public async Task ObtenerDetalleAsync_ServicioNoExiste_RegresaNoEncontrado()
    {
        // Arrange: el repositorio ya filtra por usuario, así que "no existe", "está
        // eliminado" y "es de otro usuario" regresan el mismo null.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _servicioRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServicioMantenimiento?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerDetalleAsync(Guid.NewGuid());

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_ServicioExiste_RegresaDatosCorrectos()
    {
        // Arrange
        var registro = Servicio(new DateOnly(2026, 11, 1), 11700);

        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _servicioRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, registro.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registro);

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerDetalleAsync(registro.Id);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(registro.Id, resultado.Datos!.Id);
        Assert.Equal("Aceite y filtros", resultado.Datos.TipoServicio);
        Assert.Equal(11700, resultado.Datos.KilometrajeServicio);
    }
}
