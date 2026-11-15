using Autiva.Application.Services;
using Autiva.Domain.Entities;

namespace Autiva.Tests;

public class CalculadoraRendimientoTests
{
    [Fact]
    public void Calcular_KmYLitrosPositivos_RegresaRendimientoRedondeado()
    {
        // Arrange
        var odometroActual = 10800;
        var odometroUltimoTanqueLleno = 10400;
        var litrosTotales = 40m;

        // Act
        var resultado = CalculadoraRendimiento.Calcular(odometroActual, odometroUltimoTanqueLleno, litrosTotales);

        // Assert
        Assert.Equal(10m, resultado);
    }

    [Fact]
    public void Calcular_KmRecorridosCero_RegresaNull()
    {
        // Arrange
        var odometroActual = 10400;
        var odometroUltimoTanqueLleno = 10400;
        var litrosTotales = 40m;

        // Act
        var resultado = CalculadoraRendimiento.Calcular(odometroActual, odometroUltimoTanqueLleno, litrosTotales);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void Calcular_LitrosTotalesCero_RegresaNull()
    {
        // Arrange: división entre cero evitada por la validación de litros <= 0
        var odometroActual = 10800;
        var odometroUltimoTanqueLleno = 10400;
        var litrosTotales = 0m;

        // Act
        var resultado = CalculadoraRendimiento.Calcular(odometroActual, odometroUltimoTanqueLleno, litrosTotales);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void Promedio_ConDatos_RegresaPromedioRedondeado()
    {
        // Arrange
        var rendimientos = new List<decimal> { 10m, 8m, 10m, 10m };

        // Act
        var resultado = CalculadoraRendimiento.Promedio(rendimientos);

        // Assert
        Assert.Equal(9.5m, resultado);
    }

    [Fact]
    public void Promedio_SinDatos_RegresaNull()
    {
        // Arrange
        var rendimientos = new List<decimal>();

        // Act
        var resultado = CalculadoraRendimiento.Promedio(rendimientos);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void VariacionPorcentaje_AmbosPromediosValidos_RegresaVariacionRedondeada()
    {
        // Arrange
        decimal? promedioMesActual = 10m;
        decimal? promedioMesAnterior = 9m;

        // Act
        var resultado = CalculadoraRendimiento.VariacionPorcentaje(promedioMesActual, promedioMesAnterior);

        // Assert
        Assert.Equal(11.11m, resultado);
    }

    [Fact]
    public void VariacionPorcentaje_FaltaPromedioMesAnterior_RegresaNull()
    {
        // Arrange
        decimal? promedioMesActual = 10m;
        decimal? promedioMesAnterior = null;

        // Act
        var resultado = CalculadoraRendimiento.VariacionPorcentaje(promedioMesActual, promedioMesAnterior);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void VariacionPorcentaje_PromedioMesAnteriorCero_RegresaNull()
    {
        // Arrange: evita división entre cero
        decimal? promedioMesActual = 10m;
        decimal? promedioMesAnterior = 0m;

        // Act
        var resultado = CalculadoraRendimiento.VariacionPorcentaje(promedioMesActual, promedioMesAnterior);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void AutonomiaEstimada_RendimientoYCapacidadValidos_RegresaAutonomiaRedondeada()
    {
        // Arrange
        decimal? rendimientoPromedioKml = 9.5m;
        decimal? capacidadTanqueL = 45m;

        // Act
        var resultado = CalculadoraRendimiento.AutonomiaEstimada(rendimientoPromedioKml, capacidadTanqueL);

        // Assert
        Assert.Equal(427.5m, resultado);
    }

    [Fact]
    public void AutonomiaEstimada_FaltaCapacidadTanque_RegresaNull()
    {
        // Arrange
        decimal? rendimientoPromedioKml = 9.5m;
        decimal? capacidadTanqueL = null;

        // Act
        var resultado = CalculadoraRendimiento.AutonomiaEstimada(rendimientoPromedioKml, capacidadTanqueL);

        // Assert
        Assert.Null(resultado);
    }

    // ---------- RecalcularRendimientos (HU-023) ----------

    private static CargaCombustible Carga(DateTime fechaHora, int odometro, decimal litros, bool tanqueLleno) => new()
    {
        Id = Guid.NewGuid(),
        FechaHora = fechaHora,
        Odometro = odometro,
        Litros = litros,
        TanqueLleno = tanqueLleno
    };

    [Fact]
    public void RecalcularRendimientos_CadenaTanqueLlenoParcialTanqueLleno_SoloCalculaElSegundoTanqueLleno()
    {
        // Arrange: tanque lleno (600 km), parcial de 15 L, tanque lleno (1000 km, 25 L).
        // Rendimiento del segundo tanque lleno: 400 km / (15 + 25) L = 10.
        var tanqueLleno1 = Carga(new DateTime(2026, 11, 1), 600, litros: 40m, tanqueLleno: true);
        var parcial = Carga(new DateTime(2026, 11, 5), 800, litros: 15m, tanqueLleno: false);
        var tanqueLleno2 = Carga(new DateTime(2026, 11, 10), 1000, litros: 25m, tanqueLleno: true);
        var cargas = new List<CargaCombustible> { tanqueLleno1, parcial, tanqueLleno2 };

        // Act
        CalculadoraRendimiento.RecalcularRendimientos(cargas);

        // Assert
        Assert.Null(tanqueLleno1.RendimientoKml);
        Assert.Null(parcial.RendimientoKml);
        Assert.Equal(10m, tanqueLleno2.RendimientoKml);
    }

    [Fact]
    public void RecalcularRendimientos_DespuesDeEditarLitrosDeLaIntermedia_CambiaElRendimientoDeLaSiguiente()
    {
        // Arrange: misma cadena, pero la parcial se edita de 15 L a 35 L.
        // Nuevo rendimiento del segundo tanque lleno: 400 km / (35 + 25) L = 6.67.
        var tanqueLleno1 = Carga(new DateTime(2026, 11, 1), 600, litros: 40m, tanqueLleno: true);
        var parcial = Carga(new DateTime(2026, 11, 5), 800, litros: 15m, tanqueLleno: false);
        var tanqueLleno2 = Carga(new DateTime(2026, 11, 10), 1000, litros: 25m, tanqueLleno: true);
        var cargas = new List<CargaCombustible> { tanqueLleno1, parcial, tanqueLleno2 };
        CalculadoraRendimiento.RecalcularRendimientos(cargas);

        // Act: edición de la carga intermedia.
        parcial.Litros = 35m;
        CalculadoraRendimiento.RecalcularRendimientos(cargas);

        // Assert
        Assert.Equal(6.67m, tanqueLleno2.RendimientoKml);
    }

    [Fact]
    public void RecalcularRendimientos_DespuesDeBorrarLaIntermedia_RecalculaConLasRestantes()
    {
        // Arrange: misma cadena, pero se borra la parcial (ya no entra a la lista).
        // Nuevo rendimiento del segundo tanque lleno: 400 km / 25 L = 16.
        var tanqueLleno1 = Carga(new DateTime(2026, 11, 1), 600, litros: 40m, tanqueLleno: true);
        var tanqueLleno2 = Carga(new DateTime(2026, 11, 10), 1000, litros: 25m, tanqueLleno: true);
        var cargasSinParcial = new List<CargaCombustible> { tanqueLleno1, tanqueLleno2 };

        // Act: recálculo tras el borrado lógico de la intermedia.
        CalculadoraRendimiento.RecalcularRendimientos(cargasSinParcial);

        // Assert
        Assert.Equal(16m, tanqueLleno2.RendimientoKml);
    }
}
