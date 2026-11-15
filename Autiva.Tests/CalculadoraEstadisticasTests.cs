using Autiva.Application.Services;

namespace Autiva.Tests;

public class CalculadoraEstadisticasTests
{
    [Theory]
    [InlineData("1M", 1)]
    [InlineData("3m", 3)]
    [InlineData("6M", 6)]
    [InlineData("1a", 12)]
    public void ObtenerMesesDelPeriodo_PeriodoValido_RetornaMesesCorrectos(string periodo, int mesesEsperados)
    {
        // Act
        var meses = CalculadoraEstadisticas.ObtenerMesesDelPeriodo(periodo);

        // Assert
        Assert.Equal(mesesEsperados, meses);
    }

    [Theory]
    [InlineData("2X")]
    [InlineData("")]
    [InlineData("1Y")]
    public void ObtenerMesesDelPeriodo_PeriodoInvalido_RetornaNull(string periodoInvalido)
    {
        // Act
        var meses = CalculadoraEstadisticas.ObtenerMesesDelPeriodo(periodoInvalido);

        // Assert
        Assert.Null(meses);
    }

    [Fact]
    public void CalcularDistanciaKm_SinRegistrosDentroDelPeriodo_RetornaNull()
    {
        // Act
        var distancia = CalculadoraEstadisticas.CalcularDistanciaKm(
            Array.Empty<int>(), new[] { 1000 });

        // Assert
        Assert.Null(distancia);
    }

    [Fact]
    public void CalcularDistanciaKm_ConRegistrosAntes_RestaElMaximoAntesDelPeriodo()
    {
        // Arrange: odómetro más alto antes del periodo es 1000; dentro, llega a 1500.
        var dentro = new[] { 1200, 1500 };
        var antes = new[] { 800, 1000 };

        // Act
        var distancia = CalculadoraEstadisticas.CalcularDistanciaKm(dentro, antes);

        // Assert
        Assert.Equal(500, distancia);
    }

    [Fact]
    public void CalcularDistanciaKm_SinRegistrosAntes_RestaElMasBajoDentroDelPeriodo()
    {
        // Arrange: sin registros antes, se usa el más bajo dentro del periodo.
        var dentro = new[] { 1200, 1500 };

        // Act
        var distancia = CalculadoraEstadisticas.CalcularDistanciaKm(dentro, Array.Empty<int>());

        // Assert
        Assert.Equal(300, distancia);
    }

    [Fact]
    public void CalcularDistanciaKm_UnSoloRegistroSinAntes_RetornaNull()
    {
        // Arrange: un solo registro dentro y ninguno antes da una resta de 0.
        var dentro = new[] { 1200 };

        // Act
        var distancia = CalculadoraEstadisticas.CalcularDistanciaKm(dentro, Array.Empty<int>());

        // Assert
        Assert.Null(distancia);
    }

    [Fact]
    public void CalcularCostoPorKm_SinDistancia_RetornaNull()
    {
        // Act
        var costoPorKm = CalculadoraEstadisticas.CalcularCostoPorKm(1000m, null);

        // Assert
        Assert.Null(costoPorKm);
    }

    [Fact]
    public void CalcularCostoPorKm_ConDistancia_RetornaRedondeadoADosDecimales()
    {
        // Act
        var costoPorKm = CalculadoraEstadisticas.CalcularCostoPorKm(1000m, 300);

        // Assert
        Assert.Equal(3.33m, costoPorKm);
    }

    [Fact]
    public void CalcularGastoProyectadoMes_CalculaProyeccionRedondeada()
    {
        // Arrange: 600 gastados en 10 días, mes de 30 días -> proyección de 1800.
        var gastoMesActual = 600m;

        // Act
        var proyeccion = CalculadoraEstadisticas.CalcularGastoProyectadoMes(gastoMesActual, 10, 30);

        // Assert
        Assert.Equal(1800m, proyeccion);
    }

    [Fact]
    public void ListarMesesDelPeriodo_RangoDeVariosMeses_RetornaCadaMesEnOrden()
    {
        // Arrange
        var desde = new DateOnly(2026, 9, 15);
        var hasta = new DateOnly(2026, 11, 14);

        // Act
        var meses = CalculadoraEstadisticas.ListarMesesDelPeriodo(desde, hasta);

        // Assert
        Assert.Equal(new[] { (2026, 9), (2026, 10), (2026, 11) }, meses);
    }

    [Fact]
    public void ListarMesesDelPeriodo_DesdeYHastaEnElMismoMes_RetornaUnSoloMes()
    {
        // Arrange
        var desde = new DateOnly(2026, 11, 1);
        var hasta = new DateOnly(2026, 11, 14);

        // Act
        var meses = CalculadoraEstadisticas.ListarMesesDelPeriodo(desde, hasta);

        // Assert
        Assert.Equal(new[] { (2026, 11) }, meses);
    }
}
