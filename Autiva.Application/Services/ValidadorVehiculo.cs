namespace Autiva.Application.Services;

/// <summary>
/// Reglas de validación de marca, modelo, año y placas, compartidas por el registro
/// (HU-004) y la edición (HU-021) de un vehículo.
/// </summary>
public static class ValidadorVehiculo
{
    public static List<string> Validar(
        string marca,
        string modelo,
        short anio,
        string placas,
        string? color,
        string? fotoUrl)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(marca))
            errores.Add("La marca es obligatoria.");
        else if (marca.Length > 50)
            errores.Add("La marca no puede superar los 50 caracteres.");

        if (string.IsNullOrWhiteSpace(modelo))
            errores.Add("El modelo es obligatorio.");
        else if (modelo.Length > 50)
            errores.Add("El modelo no puede superar los 50 caracteres.");

        var anioMaximo = DateTime.UtcNow.Year + 1;

        if (anio < 1950 || anio > anioMaximo)
            errores.Add($"El año debe estar entre 1950 y {anioMaximo}.");

        if (string.IsNullOrWhiteSpace(placas))
            errores.Add("Las placas son obligatorias.");
        else if (placas.Length > 15)
            errores.Add("Las placas no pueden superar los 15 caracteres.");

        if (color is { Length: > 30 })
            errores.Add("El color no puede superar los 30 caracteres.");

        if (fotoUrl is { Length: > 500 })
            errores.Add("La URL de la foto no puede superar los 500 caracteres.");

        return errores;
    }
}
