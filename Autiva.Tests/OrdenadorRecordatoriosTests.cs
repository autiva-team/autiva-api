using Autiva.Application.Services;
using Autiva.Domain.Entities;

namespace Autiva.Tests;

public class OrdenadorRecordatoriosTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 14);

    private static Vehiculo Vehiculo(int kilometrajeInicial, int odometroActual, int diasDesdeAlta) => new()
    {
        KilometrajeInicial = kilometrajeInicial,
        OdometroActual = odometroActual,
        CreadoEn = Hoy.AddDays(-diasDesdeAlta).ToDateTime(TimeOnly.MinValue)
    };

    private static Recordatorio PorFecha(DateOnly fechaLimite) => new()
    {
        Id = Guid.NewGuid(),
        Criterio = "por_fecha",
        FechaLimite = fechaLimite
    };

    private static Recordatorio PorKilometraje(int kmObjetivo, Vehiculo vehiculo) => new()
    {
        Id = Guid.NewGuid(),
        Criterio = "por_kilometraje",
        KmObjetivo = kmObjetivo,
        Vehiculo = vehiculo
    };

    [Fact]
    public void OrdenarPorUrgencia_FechaLejanaContraKilometrajeQueVencePronto_ElDeKilometrajeQuedaPrimero()
    {
        // Arrange: recordatorio por fecha a 6 meses (180 días); recordatorio por
        // kilometraje a 50 km, con un vehículo que recorre 10 km/día (5 días) -> el de
        // kilometraje es más urgente aunque la fecha "parezca" cercana en la lista.
        var porFecha = PorFecha(Hoy.AddDays(180));
        var vehiculoRapido = Vehiculo(kilometrajeInicial: 0, odometroActual: 1000, diasDesdeAlta: 100); // 10 km/día
        var porKilometraje = PorKilometraje(kmObjetivo: 1050, vehiculoRapido); // 50 km restantes

        // Act
        var resultado = OrdenadorRecordatorios.OrdenarPorUrgencia(new[] { porFecha, porKilometraje }, Hoy);

        // Assert
        Assert.Equal(porKilometraje.Id, resultado[0].Id);
        Assert.Equal(porFecha.Id, resultado[1].Id);
    }

    [Fact]
    public void OrdenarPorUrgencia_RecordatorioVencido_QuedaPrimeroQueUnoVigente()
    {
        // Arrange: uno vencido hace 5 días (días negativos) y otro vigente a 10 días.
        var vencido = PorFecha(Hoy.AddDays(-5));
        var vigente = PorFecha(Hoy.AddDays(10));

        // Act
        var resultado = OrdenadorRecordatorios.OrdenarPorUrgencia(new[] { vigente, vencido }, Hoy);

        // Assert
        Assert.Equal(vencido.Id, resultado[0].Id);
        Assert.Equal(vigente.Id, resultado[1].Id);
    }

    [Fact]
    public void OrdenarPorUrgencia_KilometrajeSinKmPorDia_QuedaAlFinal()
    {
        // Arrange: el vehículo del recordatorio por kilometraje no tiene recorrido
        // (odómetro actual igual al inicial) -> sin km por día, no se puede estimar.
        var porFecha = PorFecha(Hoy.AddDays(30));
        var vehiculoSinRecorrido = Vehiculo(kilometrajeInicial: 1000, odometroActual: 1000, diasDesdeAlta: 100);
        var porKilometraje = PorKilometraje(kmObjetivo: 1500, vehiculoSinRecorrido);

        // Act
        var resultado = OrdenadorRecordatorios.OrdenarPorUrgencia(new[] { porKilometraje, porFecha }, Hoy);

        // Assert
        Assert.Equal(porFecha.Id, resultado[0].Id);
        Assert.Equal(porKilometraje.Id, resultado[1].Id);
    }
}
