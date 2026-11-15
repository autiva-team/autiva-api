using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Ordena recordatorios pendientes por urgencia ("días para vencer"), reutilizado por
/// el resumen del vehículo (HU-013) y las listas de recordatorios (HU-017). Antes solo
/// se ordenaba por criterio (fecha siempre antes que kilometraje) sin comparar qué tan
/// pronto vence cada uno; el repositorio solo trae los datos, el orden se calcula aquí.
/// </summary>
public static class OrdenadorRecordatorios
{
    /// <summary>
    /// Ordena de menor a mayor "días para vencer" (los vencidos, con días negativos,
    /// quedan primero); los que no se pueden estimar (por kilometraje, sin km por día
    /// del vehículo) quedan al final.
    /// </summary>
    public static IReadOnlyList<Recordatorio> OrdenarPorUrgencia(IEnumerable<Recordatorio> recordatorios, DateOnly hoyMexico)
    {
        return recordatorios
            .Select(r => (Recordatorio: r, DiasParaVencer: DiasParaVencer(r, hoyMexico)))
            .OrderBy(x => x.DiasParaVencer is null)
            .ThenBy(x => x.DiasParaVencer)
            .Select(x => x.Recordatorio)
            .ToList();
    }

    private static decimal? DiasParaVencer(Recordatorio recordatorio, DateOnly hoyMexico)
    {
        if (recordatorio.Criterio == "por_fecha" && recordatorio.FechaLimite.HasValue)
            return recordatorio.FechaLimite.Value.DayNumber - hoyMexico.DayNumber;

        if (recordatorio.Criterio == "por_kilometraje" && recordatorio.KmObjetivo.HasValue)
            return DiasParaVencerPorKilometraje(recordatorio, hoyMexico);

        return null;
    }

    /// <summary>
    /// Días para vencer de un recordatorio por kilometraje: km restantes entre el km
    /// por día del vehículo (HU-024), usando su kilometraje inicial y su fecha de alta
    /// como referencia de recorrido. Null si el vehículo no tiene km por día.
    /// </summary>
    private static decimal? DiasParaVencerPorKilometraje(Recordatorio recordatorio, DateOnly hoyMexico)
    {
        var vehiculo = recordatorio.Vehiculo;
        var kmPorDia = CalculadoraAvanceServicio.KmPorDia(
            vehiculo.KilometrajeInicial,
            vehiculo.OdometroActual,
            DateOnly.FromDateTime(vehiculo.CreadoEn),
            hoyMexico.ToDateTime(TimeOnly.MinValue));

        if (kmPorDia is null or 0m)
            return null;

        var kmRestantes = recordatorio.KmObjetivo!.Value - vehiculo.OdometroActual;
        return kmRestantes / kmPorDia.Value;
    }
}
