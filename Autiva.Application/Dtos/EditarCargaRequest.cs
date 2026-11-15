namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para editar una carga de combustible. Mismos campos que el
/// registro, sin id (la carga ya existe).
/// </summary>
public class EditarCargaRequest
{
    /// <summary>Catálogo <c>tipo_combustible</c>.</summary>
    public byte TipoCombustibleId { get; set; }

    /// <summary>Litros cargados (mayor a cero).</summary>
    public decimal Litros { get; set; }

    /// <summary>Costo total pagado (mayor a cero).</summary>
    public decimal CostoTotal { get; set; }

    /// <summary>Lectura del odómetro al momento de la carga.</summary>
    public int Odometro { get; set; }

    /// <summary>Si se llenó el tanque por completo (afecta el cálculo de rendimiento).</summary>
    public bool TanqueLleno { get; set; } = true;

    /// <summary>Fecha y hora de la carga en UTC; si no viene, se usa la hora actual UTC.</summary>
    public DateTime? FechaHora { get; set; }

    /// <summary>Nombre de la gasolinera (opcional, máx. 100).</summary>
    public string? Gasolinera { get; set; }

    /// <summary>Folio del ticket o factura (opcional, máx. 50).</summary>
    public string? Folio { get; set; }

    /// <summary>Notas libres (opcional, máx. 300).</summary>
    public string? Notas { get; set; }
}
