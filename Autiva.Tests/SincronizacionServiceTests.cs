using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;
using Xunit;

namespace Autiva.Tests;

public class SincronizacionServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();

    private readonly Mock<ISincronizacionRepository> _sincronizacionRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ICargaRepository> _cargaRepository = new();

    private SincronizacionService CrearServicio() => new(
        _sincronizacionRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object,
        _cargaRepository.Object);

    private static Vehiculo VehiculoPrueba(int odometro = 5000) => new()
    {
        Id = VehiculoId,
        UsuarioId = UsuarioId,
        Marca = "Mazda",
        Modelo = "3",
        OdometroActual = odometro
    };

    [Fact]
    public async Task Sincronizar_UsuarioNoAutenticado_RetornaNoAutorizado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        var resultado = await servicio.SincronizarAsync(new SincronizacionLoteRequest());

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task Sincronizar_CargasYServicios_InsertaRegistrosYActualizaOdometro()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoPrueba(5000));

        var cargaId = Guid.NewGuid();
        var servicioId = Guid.NewGuid();

        _sincronizacionRepository
            .Setup(r => r.ExisteCargaAsync(cargaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sincronizacionRepository
            .Setup(r => r.ExisteServicioAsync(servicioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sincronizacionRepository
            .Setup(r => r.GuardarCargaAsync(It.IsAny<CargaCombustible>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _sincronizacionRepository
            .Setup(r => r.GuardarServicioAsync(It.IsAny<ServicioMantenimiento>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lote = new SincronizacionLoteRequest
        {
            Cargas = new List<CargaSincronizacionDto>
            {
                new()
                {
                    Id = cargaId,
                    VehiculoId = VehiculoId,
                    TipoCombustibleId = 1,
                    FechaHora = DateTime.UtcNow,
                    Odometro = 5500,
                    Litros = 40,
                    CostoTotal = 950m
                }
            },
            Servicios = new List<ServicioSincronizacionDto>
            {
                new()
                {
                    Id = servicioId,
                    VehiculoId = VehiculoId,
                    TipoServicioId = 1,
                    Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
                    KilometrajeServicio = 5800,
                    Costo = 1200m
                }
            }
        };

        var servicio = CrearServicio();
        var resultado = await servicio.SincronizarAsync(lote);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal(1, resultado.Datos.CargasInsertadas);
        Assert.Equal(1, resultado.Datos.ServiciosInsertados);
        Assert.Equal(0, resultado.Datos.CargasIgnoradasDuplicadas);
        Assert.Equal(0, resultado.Datos.ServiciosIgnoradosDuplicados);

        _sincronizacionRepository.Verify(r => r.GuardarCargaAsync(It.Is<CargaCombustible>(c => c.Id == cargaId), It.IsAny<CancellationToken>()), Times.Once);
        _sincronizacionRepository.Verify(r => r.GuardarServicioAsync(It.Is<ServicioMantenimiento>(s => s.Id == servicioId), It.IsAny<CancellationToken>()), Times.Once);

        // Se debió actualizar el odómetro al mayor kilometraje
        _vehiculoRepository.Verify(r => r.ActualizarOdometroAsync(UsuarioId, VehiculoId, 5500, It.IsAny<CancellationToken>()), Times.Once);
        _vehiculoRepository.Verify(r => r.ActualizarOdometroAsync(UsuarioId, VehiculoId, 5800, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sincronizar_RegistrosYaExistentes_IgnoraDeFormaIdempotente()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoPrueba(5000));

        var cargaExistenteId = Guid.NewGuid();

        _sincronizacionRepository
            .Setup(r => r.ExisteCargaAsync(cargaExistenteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lote = new SincronizacionLoteRequest
        {
            Cargas = new List<CargaSincronizacionDto>
            {
                new()
                {
                    Id = cargaExistenteId,
                    VehiculoId = VehiculoId,
                    Odometro = 5200,
                    Litros = 20,
                    CostoTotal = 500m
                }
            }
        };

        var servicio = CrearServicio();
        var resultado = await servicio.SincronizarAsync(lote);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal(0, resultado.Datos.CargasInsertadas);
        Assert.Equal(1, resultado.Datos.CargasIgnoradasDuplicadas);
        Assert.Contains(cargaExistenteId, resultado.Datos.CargasSincronizadasIds);

        _sincronizacionRepository.Verify(r => r.GuardarCargaAsync(It.IsAny<CargaCombustible>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SincronizarAsync_CargaConLitrosCero_QuedaEnFallidosYLasDemasSeGuardan()
    {
        // Arrange: una carga inválida (litros 0) y otra válida, mismo vehículo.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoPrueba(5000));
        _sincronizacionRepository
            .Setup(r => r.GuardarCargaAsync(It.IsAny<CargaCombustible>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var idMala = Guid.NewGuid();
        var idBuena = Guid.NewGuid();

        var lote = new SincronizacionLoteRequest
        {
            Cargas = new List<CargaSincronizacionDto>
            {
                new() { Id = idMala, VehiculoId = VehiculoId, Odometro = 5500, Litros = 0m, CostoTotal = 500m },
                new() { Id = idBuena, VehiculoId = VehiculoId, Odometro = 5500, Litros = 40m, CostoTotal = 800m }
            }
        };

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.SincronizarAsync(lote);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(1, resultado.Datos!.CargasInsertadas);
        Assert.Contains(idBuena, resultado.Datos.CargasSincronizadasIds);
        Assert.Single(resultado.Datos.Fallidos);
        Assert.Equal(idMala, resultado.Datos.Fallidos[0].Id);
        Assert.Equal("carga", resultado.Datos.Fallidos[0].Tipo);
        _sincronizacionRepository.Verify(
            r => r.GuardarCargaAsync(It.Is<CargaCombustible>(c => c.Id == idBuena), It.IsAny<CancellationToken>()), Times.Once);
        _sincronizacionRepository.Verify(
            r => r.GuardarCargaAsync(It.Is<CargaCombustible>(c => c.Id == idMala), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SincronizarAsync_VehiculoAjeno_QuedaEnFallidos()
    {
        // Arrange: el vehículo de la carga no existe (o no es del usuario).
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var vehiculoAjenoId = Guid.NewGuid();
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, vehiculoAjenoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);

        var cargaId = Guid.NewGuid();
        var lote = new SincronizacionLoteRequest
        {
            Cargas = new List<CargaSincronizacionDto>
            {
                new() { Id = cargaId, VehiculoId = vehiculoAjenoId, Odometro = 100, Litros = 20m, CostoTotal = 400m }
            }
        };

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.SincronizarAsync(lote);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(0, resultado.Datos!.CargasInsertadas);
        Assert.Single(resultado.Datos.Fallidos);
        Assert.Equal(cargaId, resultado.Datos.Fallidos[0].Id);
        Assert.Equal("No se encontró el vehículo.", resultado.Datos.Fallidos[0].Motivo);
        _sincronizacionRepository.Verify(r => r.GuardarCargaAsync(It.IsAny<CargaCombustible>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SincronizarAsync_CargaConIdVacio_QuedaEnFallidos()
    {
        // Arrange
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var lote = new SincronizacionLoteRequest
        {
            Cargas = new List<CargaSincronizacionDto>
            {
                new() { Id = Guid.Empty, VehiculoId = VehiculoId, Odometro = 100, Litros = 20m, CostoTotal = 400m }
            }
        };

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.SincronizarAsync(lote);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Single(resultado.Datos!.Fallidos);
        Assert.Equal(Guid.Empty, resultado.Datos.Fallidos[0].Id);
        Assert.Equal("carga", resultado.Datos.Fallidos[0].Tipo);
        // Ni siquiera se debió consultar el vehículo: el id vacío se rechaza primero.
        _vehiculoRepository.Verify(r => r.ObtenerDetalleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SincronizarAsync_CargaTanqueLlenoConAnterior_CalculaRendimiento()
    {
        // Arrange: carga anterior de tanque lleno en odómetro 4600; la nueva llega en
        // 5000 con 40 litros, sin cargas de por medio -> rendimiento = 400/40 = 10.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoPrueba(4600));

        var fechaAnterior = DateTime.UtcNow.AddDays(-10);
        var anterior = new CargaCombustible { Id = Guid.NewGuid(), VehiculoId = VehiculoId, Odometro = 4600, FechaHora = fechaAnterior, TanqueLleno = true };

        _cargaRepository
            .Setup(r => r.ObtenerUltimaTanqueLlenoAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anterior);
        _cargaRepository
            .Setup(r => r.SumarLitrosPosterioresAsync(VehiculoId, fechaAnterior, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _sincronizacionRepository
            .Setup(r => r.GuardarCargaAsync(It.IsAny<CargaCombustible>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cargaId = Guid.NewGuid();
        var lote = new SincronizacionLoteRequest
        {
            Cargas = new List<CargaSincronizacionDto>
            {
                new() { Id = cargaId, VehiculoId = VehiculoId, Odometro = 5000, Litros = 40m, CostoTotal = 800m, TanqueLleno = true }
            }
        };

        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.SincronizarAsync(lote);

        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(1, resultado.Datos!.CargasInsertadas);
        _sincronizacionRepository.Verify(
            r => r.GuardarCargaAsync(It.Is<CargaCombustible>(c => c.Id == cargaId && c.RendimientoKml == 10m), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
