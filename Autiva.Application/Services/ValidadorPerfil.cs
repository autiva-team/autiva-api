using System.Net.Mail;

namespace Autiva.Application.Services;

/// <summary>
/// Reglas de nombre y correo, compartidas por el registro (HU-001) y la edición de
/// perfil (HU-030), para no duplicarlas.
/// </summary>
public static class ValidadorPerfil
{
    public static List<string> Validar(string nombre, string correo)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(nombre))
            errores.Add("El nombre es obligatorio.");
        else if (nombre.Length > 120)
            errores.Add("El nombre no puede superar los 120 caracteres.");

        if (string.IsNullOrWhiteSpace(correo))
            errores.Add("El correo es obligatorio.");
        else if (correo.Length > 150)
            errores.Add("El correo no puede superar los 150 caracteres.");
        else if (!MailAddress.TryCreate(correo, out _))
            errores.Add("El correo no tiene un formato válido.");

        return errores;
    }
}
