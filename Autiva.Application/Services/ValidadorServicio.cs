namespace Autiva.Application.Services;

/// <summary>
/// Reglas de validación de un servicio de mantenimiento, compartidas por el registro
/// normal (HU-011) y la sincronización offline (HU-018).
/// </summary>
public static class ValidadorServicio
{
    public static List<string> Validar(
        decimal costo,
        int kilometrajeServicio,
        int kilometrajeInicialVehiculo,
        int? metaProximoKm,
        DateOnly fecha,
        DateOnly? fechaRetorno)
    {
        var errores = new List<string>();

        if (costo < 0)
            errores.Add("El costo no puede ser negativo.");

        if (kilometrajeServicio < 0)
            errores.Add("El kilometraje del servicio no puede ser negativo.");
        else if (kilometrajeServicio < kilometrajeInicialVehiculo)
            errores.Add($"El kilometraje del servicio no puede ser menor al inicial del vehículo ({kilometrajeInicialVehiculo} km).");

        if (metaProximoKm is not null && metaProximoKm <= kilometrajeServicio)
            errores.Add("La meta del próximo servicio debe ser mayor al kilometraje del servicio.");

        if (fechaRetorno is not null && fechaRetorno < fecha)
            errores.Add("La fecha de retorno no puede ser anterior a la fecha del servicio.");

        if (fecha > DateOnly.FromDateTime(DateTime.UtcNow))
            errores.Add("La fecha del servicio no puede estar en el futuro.");

        return errores;
    }
}
