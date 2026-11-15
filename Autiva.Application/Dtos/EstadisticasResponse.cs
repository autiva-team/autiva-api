namespace Autiva.Application.Dtos;

/// <summary>
/// Estadísticas de costo y eficiencia de un vehículo en un periodo (HU-028).
/// </summary>
public class EstadisticasResponse
{
    public string Periodo { get; set; } = null!;
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }

    /// <summary>Suma de la vista vw_gastos_consolidados en el periodo; 0 si no hay registros.</summary>
    public decimal GastoTotal { get; set; }

    /// <summary>Km recorridos en el periodo; null si no hay registros con odómetro o la distancia da 0.</summary>
    public int? DistanciaKm { get; set; }

    /// <summary>GastoTotal entre DistanciaKm; null si no hay distancia.</summary>
    public decimal? CostoPorKm { get; set; }

    /// <summary>Variación porcentual de CostoPorKm contra el periodo anterior del mismo tamaño; null si falta alguno.</summary>
    public decimal? VariacionCostoPorKm { get; set; }

    /// <summary>Un elemento por cada mes del periodo, en orden, para la gráfica.</summary>
    public List<RendimientoMensualDto> RendimientoMensual { get; set; } = new();

    /// <summary>Promedio de rendimiento_kml de las cargas del periodo; null si no hay.</summary>
    public decimal? RendimientoPromedio { get; set; }

    /// <summary>Proyección del gasto del mes actual (no del periodo); 0 si no hay gastos este mes.</summary>
    public decimal GastoProyectadoMes { get; set; }

    /// <summary>Misma regla de DistanciaKm, con ventana fija de los últimos 30 días.</summary>
    public int? DistanciaUltimos30Dias { get; set; }
}

/// <summary>Rendimiento promedio de un mes del periodo.</summary>
public class RendimientoMensualDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal? KmL { get; set; }
}
