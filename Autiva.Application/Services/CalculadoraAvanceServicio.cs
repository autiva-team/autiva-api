namespace Autiva.Application.Services;

/// <summary>
/// Cálculos del avance hacia el próximo servicio (HU-024). El km por día es público
/// porque también lo reutiliza la HU-026 para ordenar recordatorios por kilometraje.
/// </summary>
public static class CalculadoraAvanceServicio
{
    /// <summary>Kilómetros que faltan para la meta; puede ser negativo si ya se pasó.</summary>
    public static int KmRestantes(int metaKm, int odometroActual) => metaKm - odometroActual;

    public static bool Vencido(int kmRestantes) => kmRestantes <= 0;

    /// <summary>
    /// Porcentaje de avance entre el kilometraje del servicio y la meta, redondeado a
    /// 1 decimal y acotado entre 0 y 100. 100 si la meta es igual o menor al kilometraje
    /// del servicio (evita dividir entre cero).
    /// </summary>
    public static decimal PorcentajeAvance(int kilometrajeServicio, int metaKm, int odometroActual)
    {
        var rango = metaKm - kilometrajeServicio;

        if (rango <= 0)
            return 100m;

        var avance = (decimal)(odometroActual - kilometrajeServicio) / rango * 100;

        return Math.Round(Math.Clamp(avance, 0m, 100m), 1);
    }

    /// <summary>
    /// Promedio de kilómetros recorridos por día desde <paramref name="fechaInicio"/> hasta
    /// <paramref name="hoy"/>. Null si no han pasado días o si no hay recorrido (evita
    /// dividir entre cero y evita un promedio sin sentido).
    /// </summary>
    public static decimal? KmPorDia(int kmInicio, int kmActual, DateOnly fechaInicio, DateTime hoy)
    {
        var dias = DateOnly.FromDateTime(hoy).DayNumber - fechaInicio.DayNumber;

        if (dias <= 0)
            return null;

        var kmRecorridos = kmActual - kmInicio;

        if (kmRecorridos <= 0)
            return null;

        return Math.Round((decimal)kmRecorridos / dias, 2);
    }

    /// <summary>
    /// Días estimados para llegar a la meta, redondeados hacia arriba; null si no hay
    /// km por día o si ya está vencido (no tiene sentido estimar una fecha futura).
    /// </summary>
    public static int? DiasEstimados(int kmRestantes, decimal? kmPorDia)
    {
        if (kmPorDia is null || kmRestantes <= 0)
            return null;

        return (int)Math.Ceiling(kmRestantes / kmPorDia.Value);
    }

    public static DateOnly? FechaEstimada(DateTime hoy, int? diasEstimados) =>
        diasEstimados is null ? null : DateOnly.FromDateTime(hoy).AddDays(diasEstimados.Value);
}
