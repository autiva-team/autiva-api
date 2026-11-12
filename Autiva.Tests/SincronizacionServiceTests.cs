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

    private SincronizacionService CrearServicio() => new(
        _sincronizacionRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

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
}
