namespace Autiva.Application.Services;

/// <summary>
/// Cálculo de rendimiento (km/L) compartido por cargas, dashboard y estadísticas.
/// </summary>
public static class CalculadoraRendimiento
{
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
}
