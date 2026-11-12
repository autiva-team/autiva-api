namespace Autiva.Application.Dtos;

/// <summary>
/// Carga de combustible registrada offline con ID generado en el cliente.
/// </summary>
public class CargaSincronizacionDto
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public byte TipoCombustibleId { get; set; } = 1;
    public DateTime FechaHora { get; set; }
    public int Odometro { get; set; }
    public decimal Litros { get; set; }
    public decimal CostoTotal { get; set; }
    public bool TanqueLleno { get; set; } = true;
    public string? Gasolinera { get; set; }
    public string? Notas { get; set; }
}
