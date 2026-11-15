namespace Autiva.Application.Dtos;

/// <summary>
/// Resumen de gastos del mes de un vehículo: total, variación contra el mes
/// anterior, desglose por categoría y costo promedio diario.
/// </summary>
public class ResumenGastosResponse
{
    public Guid VehiculoId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }

    /// <summary>Suma de montos del mes consultado; 0 si no hay gastos.</summary>
    public decimal TotalMes { get; set; }

    /// <summary>Suma de montos del mes inmediato anterior; 0 si no hay gastos.</summary>
    public decimal TotalMesAnterior { get; set; }

    /// <summary>Variación porcentual de <see cref="TotalMes"/> contra <see cref="TotalMesAnterior"/>; null si el mes anterior fue 0.</summary>
    public decimal? VariacionPorcentaje { get; set; }

    /// <summary>
    /// TotalMes entre los días del mes; si es el mes actual, entre los días
    /// transcurridos hasta hoy (hora de México).
    /// </summary>
    public decimal CostoPromedioDiario { get; set; }

    public List<DesgloseCategoriaDto> Desglose { get; set; } = new();
}

/// <summary>Monto y porcentaje del total de una categoría de gasto.</summary>
public class DesgloseCategoriaDto
{
    public string Categoria { get; set; } = null!;
    public decimal Monto { get; set; }
    public decimal Porcentaje { get; set; }
}
