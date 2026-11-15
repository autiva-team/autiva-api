namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para registrar un gasto de otra categoría (lavado, peajes,
/// tenencia, multas, accesorios, etc.).
/// </summary>
public class RegistrarGastoRequest
{
    /// <summary>Id generado en el dispositivo (offline-first); si no viene, se genera en el servidor.</summary>
    public Guid? Id { get; set; }

    /// <summary>Catálogo <c>categoria_gasto</c>.</summary>
    public byte CategoriaId { get; set; }

    /// <summary>Descripción del gasto (máx. 120).</summary>
    public string Concepto { get; set; } = string.Empty;

    /// <summary>Monto pagado (mayor a cero).</summary>
    public decimal Monto { get; set; }

    /// <summary>Fecha del gasto; si no viene, se usa hoy en hora de México.</summary>
    public DateOnly? Fecha { get; set; }

    /// <summary>Lectura del odómetro al momento del gasto (opcional).</summary>
    public int? Odometro { get; set; }
}
