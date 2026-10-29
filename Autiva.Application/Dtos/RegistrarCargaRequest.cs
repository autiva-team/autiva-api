namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para registrar una carga de combustible.
/// </summary>
public class RegistrarCargaRequest
{
    /// <summary>Id generado en el dispositivo (offline-first); si no viene, se genera en el servidor.</summary>
    public Guid? Id { get; set; }

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
