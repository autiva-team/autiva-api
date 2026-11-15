namespace Autiva.Application.Services;

/// <summary>
/// Reglas de validación de un gasto de otra categoría (HU-020).
/// </summary>
public static class ValidadorGasto
{
    public static List<string> Validar(string concepto, decimal monto, DateOnly fecha, int? odometro)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(concepto))
            errores.Add("El concepto es obligatorio.");
        else if (concepto.Length > 120)
            errores.Add("El concepto no puede superar los 120 caracteres.");

        if (monto <= 0)
            errores.Add("El monto debe ser mayor a cero.");

        // La fecha se compara en hora de México, igual que el mes actual del resumen (HU-019).
        if (fecha > DateOnly.FromDateTime(ZonaMexico.Ahora()))
            errores.Add("La fecha del gasto no puede estar en el futuro.");

        if (odometro < 0)
            errores.Add("El odómetro no puede ser negativo.");

        return errores;
    }
}
