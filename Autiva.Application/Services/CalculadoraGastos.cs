using Autiva.Application.Dtos;

namespace Autiva.Application.Services;

/// <summary>
/// Cálculos de gastos por mes, compartidos por el resumen de gastos (HU-019) y
/// las estadísticas del vehículo (HU-028).
/// </summary>
public static class CalculadoraGastos
{
    /// <summary>Suma de montos; 0 si la colección está vacía.</summary>
    public static decimal TotalMonto(IEnumerable<decimal> montos) => montos.Sum();

    /// <summary>
    /// Diferencia porcentual de <paramref name="totalMes"/> contra <paramref name="totalMesAnterior"/>,
    /// redondeada a 2 decimales; null si el mes anterior fue 0 (no hay base de comparación).
    /// </summary>
    public static decimal? VariacionPorcentaje(decimal totalMes, decimal totalMesAnterior)
    {
        if (totalMesAnterior == 0)
            return null;

        return Math.Round((totalMes - totalMesAnterior) / totalMesAnterior * 100, 2);
    }

    /// <summary>
    /// Agrupa los gastos por categoría con su monto y porcentaje del total, de mayor a
    /// menor monto; lista vacía si no hay gastos.
    /// </summary>
    public static List<DesgloseCategoriaDto> Desglosar(
        IEnumerable<(string Categoria, decimal Monto)> gastos, decimal totalMes)
    {
        return gastos
            .GroupBy(g => g.Categoria)
            .Select(g =>
            {
                var monto = g.Sum(x => x.Monto);
                return new DesgloseCategoriaDto
                {
                    Categoria = g.Key,
                    Monto = monto,
                    Porcentaje = totalMes == 0 ? 0 : Math.Round(monto / totalMes * 100, 2)
                };
            })
            .OrderByDescending(d => d.Monto)
            .ToList();
    }

    /// <summary>Costo total dividido entre los días indicados; 0 si los días son 0 o menos.</summary>
    public static decimal CostoPromedioDiario(decimal totalMes, int dias) =>
        dias <= 0 ? 0 : Math.Round(totalMes / dias, 2);
}
