namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para actualizar el odómetro actual del vehículo.
/// </summary>
public class ActualizarOdometroRequest
{
    /// <summary>Nuevo valor del odómetro (no puede ser menor al actual).</summary>
    public int OdometroActual { get; set; }
}
