namespace Autiva.Application.Dtos;

/// <summary>
/// Rendimiento (km/L) y gasto de combustible de un vehículo, para el
/// dashboard y pantallas de estadísticas.
/// </summary>
public class RendimientoResponse
{
    /// <summary>Promedio de rendimiento_kml de las cargas del vehículo que lo tengan; null si ninguna.</summary>
    public decimal? RendimientoPromedioKml { get; set; }

    /// <summary>
    /// Diferencia en % entre el promedio de rendimiento del mes actual y el del mes
    /// anterior (hora de México); null si falta alguno de los dos.
    /// </summary>
    public decimal? VariacionMesAnteriorPorcentaje { get; set; }

    /// <summary>Suma de costo_total de las cargas del mes actual (hora de México); 0 si no hay ninguna.</summary>
    public decimal GastoMesActual { get; set; }

    /// <summary>Rendimiento promedio x capacidad del tanque; null si falta alguno de los dos.</summary>
    public decimal? AutonomiaEstimadaKm { get; set; }

    /// <summary>Total de cargas no eliminadas del vehículo.</summary>
    public int TotalCargas { get; set; }
}
