namespace Autiva.Application.Services;

/// <summary>
/// Reglas de validación de una carga de combustible, compartidas por el registro
/// normal (HU-008) y la sincronización offline (HU-018).
/// </summary>
public static class ValidadorCarga
{
    public static List<string> Validar(
        decimal litros,
        decimal costoTotal,
        int odometro,
        int odometroActualVehiculo,
        DateTime fechaHora)
    {
        var errores = new List<string>();

        if (litros <= 0)
            errores.Add("Los litros deben ser mayores a cero.");

        if (costoTotal <= 0)
            errores.Add("El costo total debe ser mayor a cero.");

        if (odometro < odometroActualVehiculo)
            errores.Add($"El odómetro no puede ser menor al actual ({odometroActualVehiculo} km).");

        // Tolerancia de 5 minutos por diferencia de reloj entre el dispositivo y el servidor.
        if (fechaHora > DateTime.UtcNow.AddMinutes(5))
            errores.Add("La fecha y hora de la carga no puede estar en el futuro.");

        return errores;
    }
}
