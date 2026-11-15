using Autiva.Application.Services;

namespace Autiva.Tests;

public class CalculadoraAvanceServicioTests
{
    // ---------- KmRestantes / Vencido ----------

    [Fact]
    public void KmRestantes_MetaNoAlcanzada_RegresaDiferenciaPositiva()
    {
        // Act
        var resultado = CalculadoraAvanceServicio.KmRestantes(metaKm: 15000, odometroActual: 12000);

        // Assert
        Assert.Equal(3000, resultado);
    }

    [Fact]
    public void KmRestantes_MetaYaPasada_RegresaNegativo()
    {
        // Act
        var resultado = CalculadoraAvanceServicio.KmRestantes(metaKm: 15000, odometroActual: 16000);

        // Assert
        Assert.Equal(-1000, resultado);
    }

    [Fact]
    public void Vencido_KmRestantesPositivo_RegresaFalse()
    {
        // Act
        var resultado = CalculadoraAvanceServicio.Vencido(kmRestantes: 100);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void Vencido_KmRestantesCeroONegativo_RegresaTrue()
    {
        // Act
        var resultado = CalculadoraAvanceServicio.Vencido(kmRestantes: 0);

        // Assert
        Assert.True(resultado);
    }

    // ---------- PorcentajeAvance ----------

    [Fact]
    public void PorcentajeAvance_AvanceNormal_RegresaPorcentajeRedondeado()
    {
        // Arrange: 2000 de 5000 km recorridos desde el servicio hasta la meta.
        // Act
        var resultado = CalculadoraAvanceServicio.PorcentajeAvance(kilometrajeServicio: 10000, metaKm: 15000, odometroActual: 12000);

        // Assert
        Assert.Equal(40.0m, resultado);
    }

    [Fact]
    public void PorcentajeAvance_YaPasoLaMeta_RegresaCienAcotado()
    {
        // Arrange: el odómetro ya superó la meta (120 % sin acotar).
        // Act
        var resultado = CalculadoraAvanceServicio.PorcentajeAvance(kilometrajeServicio: 10000, metaKm: 15000, odometroActual: 16000);

        // Assert
        Assert.Equal(100m, resultado);
    }

    [Fact]
    public void PorcentajeAvance_MetaIgualAlKilometrajeDelServicio_RegresaCienSinDividirEntreCero()
    {
        // Arrange: rango = metaKm - kilometrajeServicio = 0.
        // Act
        var resultado = CalculadoraAvanceServicio.PorcentajeAvance(kilometrajeServicio: 10000, metaKm: 10000, odometroActual: 10500);

        // Assert
        Assert.Equal(100m, resultado);
    }

    // ---------- KmPorDia ----------

    [Fact]
    public void KmPorDia_ConRecorridoYDias_RegresaPromedioRedondeado()
    {
        // Arrange: 500 km en 10 días.
        var fechaInicio = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var hoy = DateTime.UtcNow;

        // Act
        var resultado = CalculadoraAvanceServicio.KmPorDia(kmInicio: 10000, kmActual: 10500, fechaInicio, hoy);

        // Assert
        Assert.Equal(50m, resultado);
    }

    [Fact]
    public void KmPorDia_CeroDiasTranscurridos_RegresaNull()
    {
        // Arrange: el servicio fue hoy mismo.
        var fechaInicio = DateOnly.FromDateTime(DateTime.UtcNow);
        var hoy = DateTime.UtcNow;

        // Act
        var resultado = CalculadoraAvanceServicio.KmPorDia(kmInicio: 10000, kmActual: 10500, fechaInicio, hoy);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void KmPorDia_SinRecorrido_RegresaNull()
    {
        // Arrange: el odómetro no avanzó desde el servicio.
        var fechaInicio = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var hoy = DateTime.UtcNow;

        // Act
        var resultado = CalculadoraAvanceServicio.KmPorDia(kmInicio: 10000, kmActual: 10000, fechaInicio, hoy);

        // Assert
        Assert.Null(resultado);
    }

    // ---------- DiasEstimados / FechaEstimada ----------

    [Fact]
    public void DiasEstimados_ConKmPorDia_RegresaDiasRedondeadosHaciaArriba()
    {
        // Arrange: 3000 km restantes a 50 km/día -> 60 días exactos.
        // Act
        var resultado = CalculadoraAvanceServicio.DiasEstimados(kmRestantes: 3000, kmPorDia: 50m);

        // Assert
        Assert.Equal(60, resultado);
    }

    [Fact]
    public void DiasEstimados_SinKmPorDia_RegresaNull()
    {
        // Act
        var resultado = CalculadoraAvanceServicio.DiasEstimados(kmRestantes: 3000, kmPorDia: null);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void DiasEstimados_YaVencido_RegresaNull()
    {
        // Arrange: no tiene sentido estimar una fecha futura si ya se pasó la meta.
        // Act
        var resultado = CalculadoraAvanceServicio.DiasEstimados(kmRestantes: -500, kmPorDia: 50m);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public void FechaEstimada_ConDiasEstimados_SumaLosDiasAHoy()
    {
        // Arrange
        var hoy = new DateTime(2026, 11, 14);

        // Act
        var resultado = CalculadoraAvanceServicio.FechaEstimada(hoy, diasEstimados: 60);

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 13), resultado);
    }

    [Fact]
    public void FechaEstimada_SinDiasEstimados_RegresaNull()
    {
        // Act
        var resultado = CalculadoraAvanceServicio.FechaEstimada(DateTime.UtcNow, diasEstimados: null);

        // Assert
        Assert.Null(resultado);
    }
}
