namespace Autiva.Application.Services;

/// <summary>
/// Reglas de la contraseña, compartidas por el registro (HU-001) y el restablecimiento
/// por recuperación (HU-027), para no duplicarlas.
/// </summary>
public static class ValidadorContrasena
{
    public static List<string> Validar(string contrasena)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(contrasena))
            errores.Add("La contraseña es obligatoria.");
        else if (contrasena.Length < 8)
            errores.Add("La contraseña debe tener al menos 8 caracteres.");
        else if (contrasena.Length > 72)
            errores.Add("La contraseña no puede superar los 72 caracteres (límite de BCrypt).");

        return errores;
    }
}
