namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para editar un vehículo del usuario autenticado. No incluye
/// odómetro (PATCH de la HU-007), kilometraje inicial ni "es principal" (PATCH de la HU-006).
/// </summary>
public class EditarVehiculoRequest
{
    /// <summary>Marca del vehículo (máx. 50).</summary>
    public string Marca { get; set; } = null!;

    /// <summary>Modelo del vehículo (máx. 50).</summary>
    public string Modelo { get; set; } = null!;

    /// <summary>Año del vehículo.</summary>
    public short Anio { get; set; }

    /// <summary>Placas del vehículo (máx. 15, únicas por usuario).</summary>
    public string Placas { get; set; } = null!;

    /// <summary>Color del vehículo (opcional, máx. 30).</summary>
    public string? Color { get; set; }

    /// <summary>URL de la foto del vehículo (opcional, máx. 500).</summary>
    public string? FotoUrl { get; set; }

    /// <summary>"activo" o "en_taller" (CK_vehiculo_estatus).</summary>
    public string Estatus { get; set; } = null!;
}
