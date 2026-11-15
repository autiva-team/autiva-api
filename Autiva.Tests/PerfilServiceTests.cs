using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;

namespace Autiva.Tests;

public class PerfilServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private PerfilService CrearServicio() => new(
        _usuarioRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

    private static Usuario UsuarioPrueba() => new()
    {
        Id = UsuarioId,
        Nombre = "Prueba HU030",
        Correo = "prueba.hu030@autiva.test",
        ContrasenaHash = "hash",
        TipoCuenta = "estandar",
        CreadoEn = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private void PrepararUsuario(Usuario usuario)
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _usuarioRepository
            .Setup(r => r.ObtenerPorIdAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
    }

    private void PrepararVehiculos(int cantidad)
    {
        var vehiculos = Enumerable.Range(0, cantidad)
            .Select(_ => new Vehiculo { Id = Guid.NewGuid(), UsuarioId = UsuarioId, Marca = "Nissan", Modelo = "Versa", Placas = "ABC-123" })
            .ToList();

        _vehiculoRepository
            .Setup(r => r.ListarAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehiculos);
    }

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
    public async Task ObtenerAsync_UsuarioYaNoExiste_RetornaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _usuarioRepository
            .Setup(r => r.ObtenerPorIdAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync();

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ObtenerAsync_DatosValidos_RetornaPerfilConNumeroDeVehiculos()
    {
        // Arrange
        PrepararUsuario(UsuarioPrueba());
        PrepararVehiculos(2);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.ObtenerAsync();

        // Assert
        Assert.True(resultado.Exito);
        var datos = resultado.Datos!;
        Assert.Equal(UsuarioId, datos.Id);
        Assert.Equal("Prueba HU030", datos.Nombre);
        Assert.Equal("prueba.hu030@autiva.test", datos.Correo);
        Assert.Equal("estandar", datos.TipoCuenta);
        Assert.Equal(2, datos.NumeroVehiculos);
    }

    // =========================================================================
    // EditarAsync
    // =========================================================================

    [Fact]
    public async Task EditarAsync_SinUsuarioAutenticado_RetornaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.EditarAsync(new EditarPerfilRequest { Nombre = "X", Correo = "x@autiva.test" });

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task EditarAsync_UsuarioYaNoExiste_RetornaNoAutorizado()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _usuarioRepository
            .Setup(r => r.ObtenerPorIdAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.EditarAsync(new EditarPerfilRequest { Nombre = "X", Correo = "x@autiva.test" });

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task EditarAsync_NombreVacio_RetornaValidacion()
    {
        // Arrange
        PrepararUsuario(UsuarioPrueba());
        var servicio = CrearServicio();
        var solicitud = new EditarPerfilRequest { Nombre = "", Correo = "nuevo@autiva.test" };

        // Act
        var resultado = await servicio.EditarAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("nombre es obligatorio"));
        _usuarioRepository.Verify(
            r => r.EditarPerfilAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EditarAsync_CorreoFormatoInvalido_RetornaValidacion()
    {
        // Arrange
        PrepararUsuario(UsuarioPrueba());
        var servicio = CrearServicio();
        var solicitud = new EditarPerfilRequest { Nombre = "Prueba", Correo = "correo-sin-formato" };

        // Act
        var resultado = await servicio.EditarAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.Contains(resultado.Errores, e => e.Contains("formato válido"));
    }

    [Fact]
    public async Task EditarAsync_CorreoEnUsoPorOtroUsuario_RetornaConflicto()
    {
        // Arrange
        PrepararUsuario(UsuarioPrueba());
        _usuarioRepository
            .Setup(r => r.ExisteCorreoEnOtroUsuarioAsync("otro@autiva.test", UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var servicio = CrearServicio();
        var solicitud = new EditarPerfilRequest { Nombre = "Prueba", Correo = "otro@autiva.test" };

        // Act
        var resultado = await servicio.EditarAsync(solicitud);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.Conflicto);
        Assert.Equal("Ese correo ya está en uso.", resultado.Mensaje);
        _usuarioRepository.Verify(
            r => r.EditarPerfilAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EditarAsync_ConSuPropioCorreo_NoValidaDuplicadoYActualiza()
    {
        // Arrange: el correo no cambia, no debe consultarse si "otro" usuario lo tiene.
        var usuario = UsuarioPrueba();
        PrepararUsuario(usuario);
        PrepararVehiculos(1);
        _usuarioRepository
            .Setup(r => r.EditarPerfilAsync(UsuarioId, "Prueba Nueva", usuario.Correo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario
            {
                Id = UsuarioId,
                Nombre = "Prueba Nueva",
                Correo = usuario.Correo,
                TipoCuenta = "estandar",
                CreadoEn = usuario.CreadoEn
            });

        var servicio = CrearServicio();
        var solicitud = new EditarPerfilRequest { Nombre = "Prueba Nueva", Correo = usuario.Correo };

        // Act
        var resultado = await servicio.EditarAsync(solicitud);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal("Prueba Nueva", resultado.Datos!.Nombre);
        _usuarioRepository.Verify(
            r => r.ExisteCorreoEnOtroUsuarioAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EditarAsync_DatosValidos_ActualizaNombreYCorreoYRetornaPerfil()
    {
        // Arrange
        PrepararUsuario(UsuarioPrueba());
        PrepararVehiculos(3);
        _usuarioRepository
            .Setup(r => r.ExisteCorreoEnOtroUsuarioAsync("nuevo@autiva.test", UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _usuarioRepository
            .Setup(r => r.EditarPerfilAsync(UsuarioId, "Nombre Nuevo", "nuevo@autiva.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario
            {
                Id = UsuarioId,
                Nombre = "Nombre Nuevo",
                Correo = "nuevo@autiva.test",
                TipoCuenta = "estandar",
                CreadoEn = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        var servicio = CrearServicio();
        var solicitud = new EditarPerfilRequest { Nombre = "  Nombre Nuevo  ", Correo = " NUEVO@autiva.test " };

        // Act
        var resultado = await servicio.EditarAsync(solicitud);

        // Assert
        Assert.True(resultado.Exito);
        var datos = resultado.Datos!;
        Assert.Equal("Nombre Nuevo", datos.Nombre);
        Assert.Equal("nuevo@autiva.test", datos.Correo);
        Assert.Equal(3, datos.NumeroVehiculos);
    }
}
