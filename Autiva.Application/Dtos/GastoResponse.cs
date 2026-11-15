namespace Autiva.Application.Dtos;

/// <summary>
/// Datos de un gasto de otra categoría devueltos al cliente.
/// </summary>
public class GastoResponse
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public string Categoria { get; set; } = null!;

    public string Concepto { get; set; } = null!;

    public decimal Monto { get; set; }

    public DateOnly Fecha { get; set; }

    public int? Odometro { get; set; }
}
