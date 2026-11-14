using Autiva.Application.Services;

namespace Autiva.Tests;

public class CalculadoraGastosTests
{
    // ---------- TotalMonto ----------

    [Fact]
    public void TotalMonto_ConMontos_RegresaSuma()
    {
        // Arrange
        var montos = new[] { 500m, 300m, 200m };

        // Act
        var resultado = CalculadoraGastos.TotalMonto(montos);

        // Assert
        Assert.Equal(1000m, resultado);
    }

    [Fact]
    public void TotalMonto_SinMontos_RegresaCero()
    {
        // Arrange
        var montos = Array.Empty<decimal>();

        // Act
        var resultado = CalculadoraGastos.TotalMonto(montos);

        // Assert
        Assert.Equal(0m, resultado);
    }

    // ---------- VariacionPorcentaje ----------

    [Fact]
    public void VariacionPorcentaje_MesAnteriorEnCero_RegresaNull()
    {
        // Act
        var resultado = CalculadoraGastos.VariacionPorcentaje(totalMes: 500m, totalMesAnterior: 0m);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void VariacionPorcentaje_ConAmbosMeses_RegresaVariacionRedondeada()
    {
        // Act
        var resultado = CalculadoraGastos.VariacionPorcentaje(totalMes: 800m, totalMesAnterior: 400m);

        // Assert
        Assert.Equal(100m, resultado);
    }

    // ---------- Desglosar ----------

    [Fact]
    public void Desglosar_ConGastos_AgrupaPorcentajeYOrdenaDeMayorAMenor()
    {
        // Arrange: combustible 800 (dos registros), mantenimiento 300, total 1100.
        var gastos = new List<(string Categoria, decimal Monto)>
        {
            ("combustible", 600m),
            ("mantenimiento", 300m),
            ("combustible", 200m)
        };

        // Act
        var resultado = CalculadoraGastos.Desglosar(gastos, totalMes: 1100m);

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.Equal("combustible", resultado[0].Categoria);
        Assert.Equal(800m, resultado[0].Monto);
        Assert.Equal(72.73m, resultado[0].Porcentaje);
        Assert.Equal("mantenimiento", resultado[1].Categoria);
        Assert.Equal(300m, resultado[1].Monto);
        Assert.Equal(27.27m, resultado[1].Porcentaje);
    }

    [Fact]
    public void Desglosar_SinGastos_RegresaListaVacia()
    {
        // Arrange
        var gastos = Array.Empty<(string Categoria, decimal Monto)>();

        // Act
        var resultado = CalculadoraGastos.Desglosar(gastos, totalMes: 0m);

        // Assert
        Assert.Empty(resultado);
    }

    // ---------- CostoPromedioDiario ----------

    [Fact]
    public void CostoPromedioDiario_DiasPositivos_RegresaPromedioRedondeado()
    {
        // Act
        var resultado = CalculadoraGastos.CostoPromedioDiario(totalMes: 300m, dias: 30);

        // Assert
        Assert.Equal(10m, resultado);
    }

    [Fact]
    public void CostoPromedioDiario_DiasCero_RegresaCero()
    {
        // Act
        var resultado = CalculadoraGastos.CostoPromedioDiario(totalMes: 300m, dias: 0);

        // Assert
        Assert.Equal(0m, resultado);
    }
}
