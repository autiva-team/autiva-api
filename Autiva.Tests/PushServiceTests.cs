using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;
using Xunit;

namespace Autiva.Tests;

public class PushServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IPushRepository> _pushRepository = new();
    private readonly Mock<IPushSender> _pushSender = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private PushService CrearServicio() => new(
        _pushRepository.Object,
        _pushSender.Object,
        _currentUserService.Object);

    [Fact]
    public void ObtenerClavePublica_RegresaClaveConfigurada()
    {
        // Arrange
        _pushSender.Setup(s => s.ObtenerClavePublica()).Returns("clave-vapid-publica");
        var servicio = CrearServicio();

        // Act
        var resultado = servicio.ObtenerClavePublica();

        // Assert
        Assert.Equal("clave-vapid-publica", resultado.ClavePublica);
    }

    [Fact]
    public async Task Suscribir_SuscripcionExistente_ActualizaDatos()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var endpoint = "https://fcm.googleapis.com/fcm/send/existente";
        var existente = new SuscripcionPush
        {
            Id = Guid.NewGuid(),
            UsuarioId = Guid.NewGuid(), // suscripción renovada desde otro usuario/dispositivo
            Endpoint = endpoint,
            ClaveP256dh = "vieja",
            ClaveAuth = "vieja",
            Activa = false
        };

        _pushRepository
            .Setup(r => r.ObtenerPorEndpointAsync(endpoint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);
        _pushRepository
            .Setup(r => r.GuardarSuscripcionAsync(It.IsAny<SuscripcionPush>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SuscripcionPush s, CancellationToken _) => s);

        var solicitud = new SuscripcionPushRequest
        {
            Endpoint = endpoint,
            ClaveP256dh = "nueva-clave",
            ClaveAuth = "nuevo-secreto"
        };

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.SuscribirAsync(solicitud);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(UsuarioId, existente.UsuarioId);
        Assert.Equal("nueva-clave", existente.ClaveP256dh);
        Assert.True(existente.Activa);
    }

    [Fact]
    public async Task Desuscribir_EndpointVacio_RegresaFalseSinLlamarRepositorio()
    {
        // Arrange
        var servicio = CrearServicio();

        // Act
        var exito = await servicio.DesuscribirAsync(" ");

        // Assert
        Assert.False(exito);
        _pushRepository.Verify(r => r.DesactivarSuscripcionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ObtenerNotificacionesAsync_UsuarioNoAutenticado_RegresaListaVacia()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerNotificacionesAsync();

        // Assert
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObtenerNotificacionesAsync_ConNotificaciones_RegresaListaMapeada()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var notificacion = new Notificacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = UsuarioId,
            Titulo = "Recordatorio",
            Mensaje = "Tu verificación vence pronto",
            FechaEnvio = DateTime.UtcNow,
            Leida = false
        };
        _pushRepository
            .Setup(r => r.ObtenerNotificacionesUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notificacion> { notificacion });
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerNotificacionesAsync();

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Recordatorio", resultado[0].Titulo);
    }

    [Fact]
    public async Task MarcarNotificacionLeidaAsync_UsuarioNoAutenticado_RegresaFalse()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var exito = await servicio.MarcarNotificacionLeidaAsync(Guid.NewGuid());

        // Assert
        Assert.False(exito);
    }

    [Fact]
    public async Task MarcarNotificacionLeidaAsync_NotificacionNoExiste_RegresaFalse()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _pushRepository
            .Setup(r => r.ObtenerNotificacionPorIdAsync(It.IsAny<Guid>(), UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notificacion?)null);
        var servicio = CrearServicio();

        // Act
        var exito = await servicio.MarcarNotificacionLeidaAsync(Guid.NewGuid());

        // Assert
        Assert.False(exito);
    }

    [Fact]
    public async Task MarcarNotificacionLeidaAsync_NotificacionExiste_RegresaTrueYMarca()
    {
        // Arrange
        var notificacionId = Guid.NewGuid();
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _pushRepository
            .Setup(r => r.ObtenerNotificacionPorIdAsync(notificacionId, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Notificacion { Id = notificacionId, UsuarioId = UsuarioId, Titulo = "T", Mensaje = "M" });
        var servicio = CrearServicio();

        // Act
        var exito = await servicio.MarcarNotificacionLeidaAsync(notificacionId);

        // Assert
        Assert.True(exito);
        _pushRepository.Verify(r => r.MarcarNotificacionLeidaAsync(notificacionId, UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Suscribir_UsuarioNoAutenticado_RetornaNoAutorizado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        var solicitud = new SuscripcionPushRequest
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/test",
            ClaveP256dh = "BCj-test",
            ClaveAuth = "auth-secret"
        };

        var resultado = await servicio.SuscribirAsync(solicitud);

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task Suscribir_EndpointInvalido_RetornaErroresDeValidacion()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var servicio = CrearServicio();

        var solicitud = new SuscripcionPushRequest
        {
            Endpoint = "no-es-url",
            ClaveP256dh = "",
            ClaveAuth = ""
        };

        var resultado = await servicio.SuscribirAsync(solicitud);

        Assert.False(resultado.Exito);
        Assert.NotEmpty(resultado.Errores);
    }

    [Fact]
    public async Task Suscribir_NuevaSuscripcion_GuardaCorrectamente()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);

        var endpoint = "https://fcm.googleapis.com/fcm/send/abc123456";
        _pushRepository
            .Setup(r => r.ObtenerPorEndpointAsync(endpoint, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SuscripcionPush?)null);

        _pushRepository
            .Setup(r => r.GuardarSuscripcionAsync(It.IsAny<SuscripcionPush>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SuscripcionPush s, CancellationToken _) => s);

        var solicitud = new SuscripcionPushRequest
        {
            Endpoint = endpoint,
            ClaveP256dh = "BCj8237testkey",
            ClaveAuth = "secretkey123",
            Dispositivo = "Chrome en Android"
        };

        var servicio = CrearServicio();
        var resultado = await servicio.SuscribirAsync(solicitud);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal(endpoint, resultado.Datos.Endpoint);
        Assert.True(resultado.Datos.Activa);

        _pushRepository.Verify(r => r.GuardarSuscripcionAsync(
            It.Is<SuscripcionPush>(s => s.UsuarioId == UsuarioId && s.Endpoint == endpoint && s.Activa),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Desuscribir_LlamaDesactivarSuscripcion()
    {
        var endpoint = "https://fcm.googleapis.com/fcm/send/abc123456";
        var servicio = CrearServicio();

        var exito = await servicio.DesuscribirAsync(endpoint);

        Assert.True(exito);
        _pushRepository.Verify(r => r.DesactivarSuscripcionAsync(endpoint, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarNotificacionUsuario_GuardaNotificacionYDespachaPush()
    {
        var suscripcion = new SuscripcionPush
        {
            Id = Guid.NewGuid(),
            UsuarioId = UsuarioId,
            Endpoint = "https://fcm.googleapis.com/fcm/send/device1",
            ClaveP256dh = "key",
            ClaveAuth = "auth",
            Activa = true
        };

        _pushRepository
            .Setup(r => r.ObtenerActivasPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SuscripcionPush> { suscripcion });

        _pushSender
            .Setup(s => s.EnviarPushAsync(suscripcion, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var servicio = CrearServicio();
        await servicio.EnviarNotificacionUsuarioAsync(
            UsuarioId,
            "Alerta de afinación",
            "Tu auto necesita afinación esta semana");

        _pushRepository.Verify(r => r.GuardarNotificacionAsync(
            It.Is<Notificacion>(n => n.UsuarioId == UsuarioId && n.Titulo == "Alerta de afinación"),
            It.IsAny<CancellationToken>()), Times.Once);

        _pushSender.Verify(s => s.EnviarPushAsync(
            suscripcion,
            "Alerta de afinación",
            "Tu auto necesita afinación esta semana",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarNotificacionUsuario_SiPushFallaInactivo_DesactivaSuscripcion()
    {
        var suscripcion = new SuscripcionPush
        {
            Id = Guid.NewGuid(),
            UsuarioId = UsuarioId,
            Endpoint = "https://fcm.googleapis.com/fcm/send/expired-device",
            ClaveP256dh = "key",
            ClaveAuth = "auth",
            Activa = true
        };

        _pushRepository
            .Setup(r => r.ObtenerActivasPorUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SuscripcionPush> { suscripcion });

        _pushSender
            .Setup(s => s.EnviarPushAsync(suscripcion, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Retorna falso para indicar suscripción no válida (ej. 410 Gone)

        var servicio = CrearServicio();
        await servicio.EnviarNotificacionUsuarioAsync(
            UsuarioId,
            "Alerta",
            "Mensaje");

        _pushRepository.Verify(r => r.DesactivarSuscripcionAsync(suscripcion.Endpoint, It.IsAny<CancellationToken>()), Times.Once);
    }
}
