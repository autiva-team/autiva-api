using System.Security.Cryptography;
using System.Text;
using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Autiva.Tests;

public class RecuperacionContrasenaServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<ITokenRecuperacionRepository> _tokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IEnviadorCorreo> _enviadorCorreo = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<RecuperacionContrasenaService>> _logger = new();

    public RecuperacionContrasenaServiceTests()
    {
        _configuration.Setup(c => c["Frontend:UrlBase"]).Returns("http://localhost:5173");
    }

    private RecuperacionContrasenaService CrearServicio() => new(
        _usuarioRepository.Object,
        _tokenRepository.Object,
        _passwordHasher.Object,
        _enviadorCorreo.Object,
        _configuration.Object,
        _logger.Object);

    private static Usuario UsuarioPrueba() => new()
    {
        Id = UsuarioId,
        Nombre = "Prueba HU027",
        Correo = "prueba.hu027@autiva.test",
        ContrasenaHash = "hash-anterior",
        TipoCuenta = "estandar"
    };

    private static string ExtraerTokenDelEnlace(string enlace) => enlace.Split("token=")[1];

    private static string CalcularHashDePrueba(string tokenPlano) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenPlano))).ToLowerInvariant();

    // =========================================================================
    // RecuperarAsync
    // =========================================================================

    [Theory]
    [InlineData("")]
    [InlineData("correo-sin-formato")]
    [InlineData(null)]
    public async Task RecuperarAsync_CorreoFormatoInvalido_RetornaValidacion(string? correoInvalido)
    {
        // Arrange
        var solicitud = new RecuperarContrasenaRequest { Correo = correoInvalido! };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RecuperarAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Validacion);
        Assert.Equal("El correo no tiene un formato válido.", resultado.Mensaje);
    }

    [Fact]
    public async Task RecuperarAsync_CorreoNoRegistrado_RetornaExitoYNoEnviaCorreoNiGuardaToken()
    {
        // Arrange
        _usuarioRepository
            .Setup(r => r.ObtenerPorCorreoAsync("inexistente@autiva.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var solicitud = new RecuperarContrasenaRequest { Correo = "inexistente@autiva.test" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RecuperarAsync(solicitud);

        // Assert: mismo mensaje de éxito que si el correo sí existiera.
        Assert.True(resultado.Exito);
        Assert.Equal(
            "Si el correo está registrado, te enviamos un enlace para restablecer tu contraseña.",
            resultado.Mensaje);
        _tokenRepository.Verify(r => r.AgregarAsync(It.IsAny<TokenRecuperacion>(), It.IsAny<CancellationToken>()), Times.Never);
        _enviadorCorreo.Verify(
            e => e.EnviarEnlaceRecuperacionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecuperarAsync_CorreoRegistrado_InvalidaPendientesYGeneraTokenConSoloElHashGuardado()
    {
        // Arrange
        var usuario = UsuarioPrueba();
        _usuarioRepository
            .Setup(r => r.ObtenerPorCorreoAsync(usuario.Correo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        TokenRecuperacion? tokenGuardado = null;
        _tokenRepository
            .Setup(r => r.AgregarAsync(It.IsAny<TokenRecuperacion>(), It.IsAny<CancellationToken>()))
            .Callback<TokenRecuperacion, CancellationToken>((t, _) => tokenGuardado = t)
            .Returns(Task.CompletedTask);

        string? enlaceEnviado = null;
        _enviadorCorreo
            .Setup(e => e.EnviarEnlaceRecuperacionAsync(usuario.Correo, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, enlace, _) => enlaceEnviado = enlace)
            .Returns(Task.CompletedTask);

        var solicitud = new RecuperarContrasenaRequest { Correo = usuario.Correo };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RecuperarAsync(solicitud);

        // Assert
        Assert.True(resultado.Exito);
        _tokenRepository.Verify(r => r.InvalidarPendientesAsync(usuario.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(tokenGuardado);
        Assert.Equal(usuario.Id, tokenGuardado.UsuarioId);
        Assert.NotNull(enlaceEnviado);

        // El token del enlace nunca debe coincidir con lo guardado: solo su hash SHA-256.
        var tokenPlanoEnviado = ExtraerTokenDelEnlace(enlaceEnviado);
        Assert.NotEqual(tokenPlanoEnviado, tokenGuardado.TokenHash);
        Assert.Equal(64, tokenGuardado.TokenHash.Length);
        Assert.Equal(CalcularHashDePrueba(tokenPlanoEnviado), tokenGuardado.TokenHash);
    }

    [Fact]
    public async Task RecuperarAsync_FallaEnvioDeCorreo_SigueRespondiendoExito()
    {
        // Arrange: si el SMTP falla, el cliente no debe notarlo (no revela el estado del correo).
        var usuario = UsuarioPrueba();
        _usuarioRepository
            .Setup(r => r.ObtenerPorCorreoAsync(usuario.Correo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _enviadorCorreo
            .Setup(e => e.EnviarEnlaceRecuperacionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP no disponible"));

        var solicitud = new RecuperarContrasenaRequest { Correo = usuario.Correo };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RecuperarAsync(solicitud);

        // Assert
        Assert.True(resultado.Exito);
    }

    // =========================================================================
    // RestablecerAsync
    // =========================================================================

    [Fact]
    public async Task RestablecerAsync_TokenVacio_RetornaValidacionEnlaceInvalido()
    {
        // Arrange
        var solicitud = new RestablecerContrasenaRequest { Token = "", NuevaContrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RestablecerAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("El enlace no es válido o ya expiró.", resultado.Mensaje);
    }

    [Fact]
    public async Task RestablecerAsync_TokenNoExiste_RetornaValidacionEnlaceInvalido()
    {
        // Arrange
        _tokenRepository
            .Setup(r => r.ObtenerPorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenRecuperacion?)null);

        var solicitud = new RestablecerContrasenaRequest { Token = "token-inventado", NuevaContrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RestablecerAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("El enlace no es válido o ya expiró.", resultado.Mensaje);
    }

    [Fact]
    public async Task RestablecerAsync_TokenYaUsado_RetornaValidacionEnlaceInvalido()
    {
        // Arrange
        var token = new TokenRecuperacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = UsuarioId,
            Usuario = UsuarioPrueba(),
            ExpiraEn = DateTime.UtcNow.AddMinutes(10),
            UsadoEn = DateTime.UtcNow.AddMinutes(-1)
        };
        _tokenRepository
            .Setup(r => r.ObtenerPorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var solicitud = new RestablecerContrasenaRequest { Token = "token-usado", NuevaContrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RestablecerAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("El enlace no es válido o ya expiró.", resultado.Mensaje);
    }

    [Fact]
    public async Task RestablecerAsync_TokenExpirado_RetornaValidacionEnlaceInvalido()
    {
        // Arrange
        var token = new TokenRecuperacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = UsuarioId,
            Usuario = UsuarioPrueba(),
            ExpiraEn = DateTime.UtcNow.AddMinutes(-1),
            UsadoEn = null
        };
        _tokenRepository
            .Setup(r => r.ObtenerPorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var solicitud = new RestablecerContrasenaRequest { Token = "token-expirado", NuevaContrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RestablecerAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("El enlace no es válido o ya expiró.", resultado.Mensaje);
    }

    [Fact]
    public async Task RestablecerAsync_ContrasenaNuevaDemasiadoCorta_RetornaValidacionConMensajeDeLaRegla()
    {
        // Arrange
        var token = new TokenRecuperacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = UsuarioId,
            Usuario = UsuarioPrueba(),
            ExpiraEn = DateTime.UtcNow.AddMinutes(10),
            UsadoEn = null
        };
        _tokenRepository
            .Setup(r => r.ObtenerPorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var solicitud = new RestablecerContrasenaRequest { Token = "token-valido", NuevaContrasena = "abc" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RestablecerAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("La contraseña debe tener al menos 8 caracteres.", resultado.Mensaje);
        _tokenRepository.Verify(
            r => r.RestablecerContrasenaAsync(It.IsAny<TokenRecuperacion>(), It.IsAny<Usuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RestablecerAsync_DatosValidos_ActualizaHashYMarcaTokenUsadoEnUnaSolaOperacion()
    {
        // Arrange
        var usuario = UsuarioPrueba();
        var token = new TokenRecuperacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Usuario = usuario,
            ExpiraEn = DateTime.UtcNow.AddMinutes(10),
            UsadoEn = null
        };
        _tokenRepository
            .Setup(r => r.ObtenerPorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _passwordHasher.Setup(h => h.Hash("Prueba123!")).Returns("hash-nuevo");

        var solicitud = new RestablecerContrasenaRequest { Token = "token-valido", NuevaContrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RestablecerAsync(solicitud);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal("Tu contraseña se actualizó.", resultado.Mensaje);
        Assert.Equal("hash-nuevo", usuario.ContrasenaHash);
        Assert.NotNull(usuario.ContrasenaActualizadaEn);
        _tokenRepository.Verify(r => r.RestablecerContrasenaAsync(token, usuario, It.IsAny<CancellationToken>()), Times.Once);
    }
}
