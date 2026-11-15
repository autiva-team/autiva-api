using Autiva.Application.Dtos;
using Autiva.Application.Interfaces;
using Autiva.Application.Services;
using Autiva.Domain.Entities;
using Moq;
using Xunit;

namespace Autiva.Tests;

public class RecordatorioConsultaTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid VehiculoId = Guid.NewGuid();
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly Mock<IRecordatorioRepository> _recordatorioRepository = new();
    private readonly Mock<IVehiculoRepository> _vehiculoRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private RecordatorioService CrearServicio() => new(
        _recordatorioRepository.Object,
        _vehiculoRepository.Object,
        _currentUserService.Object);

    private static Vehiculo VehiculoPrueba(int odometroActual = 10000) => new()
    {
        Id = VehiculoId,
        UsuarioId = UsuarioId,
        Marca = "Nissan",
        Modelo = "Versa",
        Anio = 2020,
        Placas = "ABC-123",
        OdometroActual = odometroActual
    };

    [Fact]
    public async Task ListarPorVehiculo_UsuarioNoAutenticado_RetornaNoAutorizado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        var resultado = await servicio.ListarPorVehiculoAsync(VehiculoId);

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ListarPorVehiculo_VehiculoNoEncontrado_RetornaNoEncontrado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehiculo?)null);

        var servicio = CrearServicio();
        var resultado = await servicio.ListarPorVehiculoAsync(VehiculoId);

        Assert.False(resultado.Exito);
        Assert.False(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ListarPorVehiculo_CalculaVencimientoYProximidadCorrectamente()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _vehiculoRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehiculoPrueba(10000));

        var recordatorioFecha = new Recordatorio
        {
            Id = Guid.NewGuid(),
            VehiculoId = VehiculoId,
            Titulo = "Verificación vehicular",
            Tipo = "verificacion",
            Criterio = "por_fecha",
            FechaLimite = Hoy.AddDays(4),
            AnticipacionDias = 7,
            Estado = "pendiente"
        };

        var recordatorioKm = new Recordatorio
        {
            Id = Guid.NewGuid(),
            VehiculoId = VehiculoId,
            Vehiculo = VehiculoPrueba(10000), // en producción siempre viene vía Include
            Titulo = "Cambio de pastillas",
            Tipo = "mantenimiento",
            Criterio = "por_kilometraje",
            KmObjetivo = 9800, // Menor al actual (10000) -> ya venció
            AnticipacionDias = 7,
            Estado = "pendiente"
        };

        _recordatorioRepository
            .Setup(r => r.ObtenerPorVehiculoAsync(VehiculoId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Recordatorio> { recordatorioFecha, recordatorioKm });

        var servicio = CrearServicio();
        var resultado = await servicio.ListarPorVehiculoAsync(VehiculoId);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal(2, resultado.Datos.Count);

        var fechaDto = resultado.Datos.First(d => d.Id == recordatorioFecha.Id);
        Assert.False(fechaDto.EstaVencido);
        Assert.Equal(4, fechaDto.DiasRestantes);
        Assert.True(fechaDto.ProximoAVencer);

        var kmDto = resultado.Datos.First(d => d.Id == recordatorioKm.Id);
        Assert.True(kmDto.EstaVencido);
        Assert.Equal(-200, kmDto.KmRestantes);
    }

    [Fact]
    public async Task ListarPorUsuario_RetornaRecordatoriosDeTodosLosVehiculos()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);

        var vehiculo = VehiculoPrueba(12000);
        var recordatorio = new Recordatorio
        {
            Id = Guid.NewGuid(),
            VehiculoId = VehiculoId,
            Vehiculo = vehiculo,
            Titulo = "Póliza anual",
            Tipo = "seguro",
            Criterio = "por_fecha",
            FechaLimite = Hoy.AddDays(15),
            AnticipacionDias = 7,
            Estado = "pendiente"
        };

        _recordatorioRepository
            .Setup(r => r.ObtenerPorUsuarioAsync(UsuarioId, "pendiente", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Recordatorio> { recordatorio });

        var servicio = CrearServicio();
        var resultado = await servicio.ListarPorUsuarioAsync("pendiente");

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Single(resultado.Datos);
        Assert.Equal("ABC-123", resultado.Datos[0].VehiculoPlacas);
    }

    [Fact]
    public async Task ConcluirAsync_UsuarioNoAutenticado_RegresaNoAutorizado()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns((Guid?)null);
        var servicio = CrearServicio();

        var resultado = await servicio.ConcluirAsync(Guid.NewGuid());

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoAutorizado);
    }

    [Fact]
    public async Task ConcluirAsync_RecordatorioNoExiste_RegresaNoEncontrado()
    {
        // Mismo resultado (null) si no existe, está borrado, su vehículo está borrado o
        // es de otro usuario: el repositorio ya filtra todos esos casos en una consulta.
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        _recordatorioRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recordatorio?)null);
        var servicio = CrearServicio();

        var resultado = await servicio.ConcluirAsync(Guid.NewGuid());

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
        Assert.Equal("No se encontró el recordatorio.", resultado.Mensaje);
    }

    [Fact]
    public async Task ConcluirAsync_YaEstabaConcluido_RegresaConflicto()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);
        var recordatorioId = Guid.NewGuid();
        var recordatorio = new Recordatorio
        {
            Id = recordatorioId,
            VehiculoId = VehiculoId,
            Vehiculo = VehiculoPrueba(),
            Titulo = "Pago tenencia",
            Criterio = "por_fecha",
            Estado = "concluido",
            FechaConcluido = DateTime.UtcNow.AddDays(-1)
        };
        _recordatorioRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, recordatorioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recordatorio);
        var servicio = CrearServicio();

        var resultado = await servicio.ConcluirAsync(recordatorioId);

        Assert.False(resultado.Exito);
        Assert.True(resultado.Conflicto);
        Assert.Equal("El recordatorio ya estaba concluido.", resultado.Mensaje);
        _recordatorioRepository.Verify(r => r.ActualizarAsync(It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Concluir_MarcaEstadoConcluidoYActualizaFecha()
    {
        _currentUserService.Setup(s => s.UsuarioId).Returns(UsuarioId);

        var recordatorioId = Guid.NewGuid();
        var recordatorio = new Recordatorio
        {
            Id = recordatorioId,
            VehiculoId = VehiculoId,
            Vehiculo = VehiculoPrueba(),
            Titulo = "Pago tenencia",
            Tipo = "tramite",
            Criterio = "por_fecha",
            Estado = "pendiente"
        };

        _recordatorioRepository
            .Setup(r => r.ObtenerDetalleAsync(UsuarioId, recordatorioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recordatorio);

        var servicio = CrearServicio();
        var resultado = await servicio.ConcluirAsync(recordatorioId);

        Assert.True(resultado.Exito);
        Assert.Equal("concluido", resultado.Datos!.Estado);
        Assert.NotNull(recordatorio.FechaConcluido);
        _recordatorioRepository.Verify(r => r.ActualizarAsync(recordatorio, It.IsAny<CancellationToken>()), Times.Once);
    }
}
