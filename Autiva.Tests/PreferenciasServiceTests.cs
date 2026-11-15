using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class PreferenciasServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IPreferenciasRepository> _preferenciasRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private PreferenciasService CrearServicio() => new(
        _preferenciasRepository.Object,
        _currentUserService.Object);

    private static PreferenciasUsuario PreferenciaGuardada() => new()
    {
        UsuarioId = UsuarioId,
        NotificacionesPush = false,
        UnidadMedida = "mpg",
        RespaldoNube = false,
        ModoOffline = true,
        UltimaSincronizacion = new DateTime(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc)
    };

    // =========================================================================
    // ObtenerAsync
    // =========================================================================

    [Fact]
    public async Task ObtenerAsync_SinUsuarioAutenticado_RetornaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync();

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ObtenerAsync_SinFilaDePreferencias_RetornaValoresPorDefecto()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _preferenciasRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PreferenciasUsuario?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync();

        // Assert
        Assert.True(resultado.Exito);
        var datos = resultado.Datos!;
        Assert.True(datos.NotificacionesPush);
        Assert.Equal("km_l", datos.UnidadMedida);
        Assert.True(datos.RespaldoNube);
        Assert.True(datos.ModoOffline);
        Assert.Null(datos.UltimaSincronizacion);
        _preferenciasRepository.Verify(
            r => r.AgregarAsync(It.IsAny<PreferenciasUsuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ObtenerAsync_ConFilaDePreferencias_RetornaValoresGuardados()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _preferenciasRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PreferenciaGuardada());
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync();

        // Assert
        var datos = resultado.Datos!;
        Assert.False(datos.NotificacionesPush);
        Assert.Equal("mpg", datos.UnidadMedida);
        Assert.False(datos.RespaldoNube);
        Assert.True(datos.ModoOffline);
        Assert.NotNull(datos.UltimaSincronizacion);
    }

    // =========================================================================
    // ActualizarAsync
    // =========================================================================

    [Fact]
    public async Task ActualizarAsync_SinUsuarioAutenticado_RetornaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ActualizarAsync(new ActualizarPreferenciasRequest { UnidadMedida = "km_l" });

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Theory]
    [InlineData("litros")]
    [InlineData("")]
    [InlineData("KM")]
    public async Task ActualizarAsync_UnidadMedidaInvalida_RetornaValidacion(string unidadInvalida)
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var servicio = CrearServicio();
        var solicitud = new ActualizarPreferenciasRequest { UnidadMedida = unidadInvalida };

        // Act
        var resultado = await servicio.ActualizarAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
        Assert.Equal("La unidad de medida debe ser \"km_l\" o \"mpg\".", resultado.Mensaje);
        _preferenciasRepository.Verify(
            r => r.AgregarAsync(It.IsAny<PreferenciasUsuario>(), It.IsAny<CancellationToken>()), Times.Never);
        _preferenciasRepository.Verify(
            r => r.ActualizarAsync(It.IsAny<PreferenciasUsuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarAsync_SinFilaExistente_CreaLaFilaNueva()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _preferenciasRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PreferenciasUsuario?)null);

        PreferenciasUsuario? creada = null;
        _preferenciasRepository
            .Setup(r => r.AgregarAsync(It.IsAny<PreferenciasUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<PreferenciasUsuario, CancellationToken>((p, _) => creada = p)
            .ReturnsAsync((PreferenciasUsuario p, CancellationToken _) => p);

        var servicio = CrearServicio();
        var solicitud = new ActualizarPreferenciasRequest
        {
            NotificacionesPush = false,
            UnidadMedida = "MPG",
            RespaldoNube = false,
            ModoOffline = false
        };

        // Act
        var resultado = await servicio.ActualizarAsync(solicitud);

        // Assert
        Assert.True(resultado.Exito);
        Assert.NotNull(creada);
        Assert.Equal(UsuarioId, creada.UsuarioId);
        Assert.Equal("mpg", creada.UnidadMedida);
        Assert.False(creada.NotificacionesPush);
        Assert.False(creada.RespaldoNube);
        Assert.False(creada.ModoOffline);
        _preferenciasRepository.Verify(
            r => r.ActualizarAsync(It.IsAny<PreferenciasUsuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarAsync_ConFilaExistente_ActualizaLosValores()
    {
        // Arrange: notificacionesPush pasa a false; no debe borrar nada, solo guardar el valor.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var existente = new PreferenciasUsuario
        {
            UsuarioId = UsuarioId,
            NotificacionesPush = true,
            UnidadMedida = "km_l",
            RespaldoNube = true,
            ModoOffline = true,
            UltimaSincronizacion = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        _preferenciasRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);
        _preferenciasRepository
            .Setup(r => r.ActualizarAsync(It.IsAny<PreferenciasUsuario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PreferenciasUsuario p, CancellationToken _) => p);

        var servicio = CrearServicio();
        var solicitud = new ActualizarPreferenciasRequest
        {
            NotificacionesPush = false,
            UnidadMedida = "mpg",
            RespaldoNube = true,
            ModoOffline = true
        };

        // Act
        var resultado = await servicio.ActualizarAsync(solicitud);

        // Assert
        var datos = resultado.Datos!;
        Assert.False(datos.NotificacionesPush);
        Assert.Equal("mpg", datos.UnidadMedida);
        // La última sincronización no se edita desde este endpoint.
        Assert.Equal(existente.UltimaSincronizacion, datos.UltimaSincronizacion);
        _preferenciasRepository.Verify(
            r => r.AgregarAsync(It.IsAny<PreferenciasUsuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
