using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtGenerator> _jwtGenerator = new();

    private AuthService CrearServicio() => new(
        _usuarioRepository.Object,
        _passwordHasher.Object,
        _jwtGenerator.Object);

    // =========================================================================
    // RegistrarAsync (HU-001)
    // =========================================================================

    [Fact]
    public async Task RegistrarAsync_NombreVacio_RetornaErrorValidacion()
    {
        // Arrange
        var solicitud = new RegistroUsuarioRequest { Nombre = "", Correo = "nuevo@autiva.test", Contrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("nombre es obligatorio"));
    }

    [Fact]
    public async Task RegistrarAsync_CorreoFormatoInvalido_RetornaErrorValidacion()
    {
        // Arrange
        var solicitud = new RegistroUsuarioRequest { Nombre = "Prueba", Correo = "correo-sin-formato", Contrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("formato válido"));
    }

    [Fact]
    public async Task RegistrarAsync_ContrasenaCorta_RetornaErrorValidacion()
    {
        // Arrange: valida que la regla reutilizada de ValidadorContrasena se aplique aquí.
        var solicitud = new RegistroUsuarioRequest { Nombre = "Prueba", Correo = "nuevo@autiva.test", Contrasena = "abc" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("al menos 8 caracteres"));
    }

    [Fact]
    public async Task RegistrarAsync_CorreoYaExiste_RetornaConflicto()
    {
        // Arrange
        _usuarioRepository
            .Setup(r => r.ExisteCorreoAsync("existente@autiva.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var solicitud = new RegistroUsuarioRequest { Nombre = "Prueba", Correo = "existente@autiva.test", Contrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Conflicto);
        Assert.Equal("Ya existe una cuenta con ese correo.", resultado.Mensaje);
    }

    [Fact]
    public async Task RegistrarAsync_DatosValidos_RegistraYRetornaExito()
    {
        // Arrange
        _usuarioRepository
            .Setup(r => r.ExisteCorreoAsync("nuevo@autiva.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _usuarioRepository
            .Setup(r => r.AgregarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _passwordHasher.Setup(h => h.Hash("Prueba123!")).Returns("hash-generado");

        var solicitud = new RegistroUsuarioRequest { Nombre = "Prueba", Correo = "nuevo@autiva.test", Contrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.RegistrarAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal("nuevo@autiva.test", resultado.Datos.Correo);
        Assert.Equal("estandar", resultado.Datos.TipoCuenta);
    }

    // =========================================================================
    // LoginAsync
    // =========================================================================

    [Fact]
    public async Task LoginAsync_CamposVacios_RetornaFallo()
    {
        // Arrange
        var solicitud = new LoginRequest { Correo = "", Contrasena = "" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.LoginAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("Correo y contraseña son obligatorios.", resultado.Mensaje);
    }

    [Fact]
    public async Task LoginAsync_UsuarioNoExiste_RetornaFallo()
    {
        // Arrange
        _usuarioRepository
            .Setup(r => r.ObtenerPorCorreoAsync("inexistente@autiva.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var solicitud = new LoginRequest { Correo = "inexistente@autiva.test", Contrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.LoginAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("Correo o contraseña incorrectos.", resultado.Mensaje);
    }

    [Fact]
    public async Task LoginAsync_ContrasenaIncorrecta_RetornaFallo()
    {
        // Arrange
        var usuario = new Usuario { Id = Guid.NewGuid(), Correo = "prueba@autiva.test", ContrasenaHash = "hash", Nombre = "Prueba", TipoCuenta = "estandar" };
        _usuarioRepository
            .Setup(r => r.ObtenerPorCorreoAsync(usuario.Correo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verify("ContrasenaMala1", "hash")).Returns(false);

        var solicitud = new LoginRequest { Correo = usuario.Correo, Contrasena = "ContrasenaMala1" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.LoginAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal("Correo o contraseña incorrectos.", resultado.Mensaje);
    }

    [Fact]
    public async Task LoginAsync_DatosValidos_RetornaTokenYDatosDelUsuario()
    {
        // Arrange
        var usuario = new Usuario { Id = Guid.NewGuid(), Correo = "prueba@autiva.test", ContrasenaHash = "hash", Nombre = "Prueba", TipoCuenta = "estandar" };
        _usuarioRepository
            .Setup(r => r.ObtenerPorCorreoAsync(usuario.Correo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verify("Prueba123!", "hash")).Returns(true);
        _jwtGenerator
            .Setup(j => j.GenerarToken(usuario))
            .Returns(("token-jwt", DateTime.UtcNow.AddHours(1)));

        var solicitud = new LoginRequest { Correo = usuario.Correo, Contrasena = "Prueba123!" };
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.LoginAsync(solicitud, CancellationToken.None);

        // Assert
        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal("token-jwt", resultado.Datos.Token);
        Assert.Equal(usuario.Id, resultado.Datos.UsuarioId);
    }
}
