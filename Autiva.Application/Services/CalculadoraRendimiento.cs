using Autiva.Domain.Entities;

namespace Autiva.Application.Services;

/// <summary>
/// Cálculo de rendimiento (km/L) compartido por cargas, dashboard y estadísticas.
/// </summary>
public static class CalculadoraRendimiento
{
    /// <summary>
    /// Recalcula <see cref="CargaCombustible.RendimientoKml"/> de cada carga de la lista,
    /// con la misma regla del registro: solo las de tanque lleno, y solo si hay una de
    /// tanque lleno anterior en la lista; si no aplica, null. Usado después de editar o
    /// borrar una carga (HU-023), porque cambia el rendimiento de las posteriores.
    /// </summary>
    /// <param name="cargasOrdenadas">Cargas no borradas del vehículo, de la más antigua a la más reciente.</param>
    public static void RecalcularRendimientos(IReadOnlyList<CargaCombustible> cargasOrdenadas)
    {
        CargaCombustible? ultimaTanqueLleno = null;
        var litrosDesdeUltimaTanqueLleno = 0m;

        foreach (var carga in cargasOrdenadas)
        {
            litrosDesdeUltimaTanqueLleno += carga.Litros;

            if (!carga.TanqueLleno)
            {
                carga.RendimientoKml = null;
                continue;
            }

            carga.RendimientoKml = ultimaTanqueLleno is null
                ? null
                : Calcular(carga.Odometro, ultimaTanqueLleno.Odometro, litrosDesdeUltimaTanqueLleno);

            ultimaTanqueLleno = carga;
            litrosDesdeUltimaTanqueLleno = 0m;
        }
    }

    /// <summary>
    /// Rendimiento entre la última carga de tanque lleno y la carga actual (también de tanque lleno).
    /// Quien llama decide si aplica (ambas cargas deben ser de tanque lleno); esta función solo
    /// hace la cuenta y regresa null si los kilómetros o los litros no son positivos.
    /// </summary>
    /// <param name="odometroActual">Odómetro de la carga que se está registrando.</param>
    /// <param name="odometroUltimoTanqueLleno">Odómetro de la última carga de tanque lleno anterior.</param>
    /// <param name="litrosTotales">Suma de litros de todas las cargas desde esa carga (exclusiva) hasta la actual (inclusiva).</param>
    public static decimal? Calcular(int odometroActual, int odometroUltimoTanqueLleno, decimal litrosTotales)
    {
        var kmRecorridos = odometroActual - odometroUltimoTanqueLleno;

        if (kmRecorridos <= 0 || litrosTotales <= 0)
            return null;

        return Math.Round(kmRecorridos / litrosTotales, 2);
    }

    /// <summary>Promedio de una serie de rendimientos; null si la serie está vacía.</summary>
    public static decimal? Promedio(IReadOnlyCollection<decimal> rendimientos)
    {
        if (rendimientos.Count == 0)
            return null;

        return Math.Round(rendimientos.Average(), 2);
    }

    /// <summary>
    /// Diferencia porcentual de <paramref name="actual"/> contra <paramref name="anterior"/>.
    /// Null si falta alguno de los dos o si el anterior es cero (no hay base de comparación).
    /// </summary>
    public static decimal? VariacionPorcentaje(decimal? actual, decimal? anterior)
    {
        if (actual is null || anterior is null || anterior == 0)
            return null;

        return Math.Round((actual.Value - anterior.Value) / anterior.Value * 100, 2);
    }

    /// <summary>
    /// Autonomía estimada: rendimiento promedio x capacidad del tanque.
    /// Null si falta alguno de los dos.
    /// </summary>
    public static decimal? AutonomiaEstimada(decimal? rendimientoPromedioKml, decimal? capacidadTanqueL)
    {
        if (rendimientoPromedioKml is null || capacidadTanqueL is null)
            return null;

        return Math.Round(rendimientoPromedioKml.Value * capacidadTanqueL.Value, 2);
    }
}
