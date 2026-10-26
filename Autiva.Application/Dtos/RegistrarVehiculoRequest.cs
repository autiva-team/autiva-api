namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para registrar un vehículo del usuario autenticado.
/// </summary>
public class RegistrarVehiculoRequest
{
    /// <summary>Marca del vehículo (máx. 50).</summary>
    public string Marca { get; set; } = null!;

    /// <summary>Modelo del vehículo (máx. 50).</summary>
    public string Modelo { get; set; } = null!;

    /// <summary>Año del vehículo.</summary>
    public short Anio { get; set; }

    /// <summary>Placas del vehículo (máx. 15, únicas por usuario).</summary>
    public string Placas { get; set; } = null!;

    /// <summary>Kilometraje inicial del odómetro.</summary>
    public int KilometrajeInicial { get; set; }

    /// <summary>Color del vehículo (opcional, máx. 30).</summary>
    public string? Color { get; set; }

    /// <summary>URL de la foto del vehículo (opcional, máx. 500).</summary>
    public string? FotoUrl { get; set; }
}
