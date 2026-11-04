using Autiva.Application.Services;

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
}
