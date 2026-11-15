namespace Autiva.Application.Services;

/// <summary>
/// Fórmulas de las estadísticas del vehículo (HU-028): periodo, distancia recorrida,
/// costo por km y proyección del gasto del mes. Sin acceso a datos, para probarse sola.
/// </summary>
public static class CalculadoraEstadisticas
{
    private static readonly Dictionary<string, int> MesesPorPeriodo = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1M"] = 1,
        ["3M"] = 3,
        ["6M"] = 6,
        ["1A"] = 12
    };

    /// <summary>Meses que representa el periodo (1M, 3M, 6M o 1A, sin importar mayúsculas); null si no es válido.</summary>
    public static int? ObtenerMesesDelPeriodo(string periodo) =>
        MesesPorPeriodo.TryGetValue(periodo, out var meses) ? meses : null;

    /// <summary>
    /// Km recorridos: el odómetro más alto dentro del periodo menos el más alto antes del
    /// periodo; si no hay registros antes, menos el más bajo dentro del periodo. Null si no
    /// hay registros dentro del periodo o si la resta da 0 (un solo registro).
    /// </summary>
    public static int? CalcularDistanciaKm(IEnumerable<int> odometrosDentro, IEnumerable<int> odometrosAntes)
    {
        var dentro = odometrosDentro.ToList();

        if (dentro.Count == 0)
            return null;

        var antes = odometrosAntes.ToList();
        var odometroBase = antes.Count > 0 ? antes.Max() : dentro.Min();
        var distancia = dentro.Max() - odometroBase;

        return distancia == 0 ? null : distancia;
    }

    /// <summary>Gasto total entre la distancia, redondeado a 2 decimales; null si no hay distancia.</summary>
    public static decimal? CalcularCostoPorKm(decimal gastoTotal, int? distanciaKm) =>
        distanciaKm is null ? null : Math.Round(gastoTotal / distanciaKm.Value, 2);

    /// <summary>
    /// Proyección del gasto del mes: gasto acumulado entre los días transcurridos, por los
    /// días totales del mes, redondeada a 2 decimales.
    /// </summary>
    public static decimal CalcularGastoProyectadoMes(decimal gastoMesActual, int diasTranscurridos, int diasDelMes) =>
        Math.Round(gastoMesActual / diasTranscurridos * diasDelMes, 2);

    /// <summary>Cada mes calendario entre <paramref name="desde"/> y <paramref name="hasta"/>, en orden.</summary>
    public static List<(int Anio, int Mes)> ListarMesesDelPeriodo(DateOnly desde, DateOnly hasta)
    {
        var meses = new List<(int Anio, int Mes)>();
        var cursor = new DateOnly(desde.Year, desde.Month, 1);
        var limite = new DateOnly(hasta.Year, hasta.Month, 1);

        while (cursor <= limite)
        {
            meses.Add((cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(1);
        }

        return meses;
    }
}
