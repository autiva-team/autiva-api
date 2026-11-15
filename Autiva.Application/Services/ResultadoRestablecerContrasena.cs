namespace Autiva.Application.Services;

/// <summary>
/// Resultado de un intento de restablecer la contraseña con un token.
/// </summary>
public class ResultadoRestablecerContrasena : IResultadoOperacion
{
    public bool Exito { get; init; }

    public bool NoAutorizado => false;

    public bool NoEncontrado => false;

    public bool Validacion { get; init; }

    public string? Mensaje { get; init; }

    public static ResultadoRestablecerContrasena CrearValidacion(string mensaje) => new()
    {
        Exito = false,
        Validacion = true,
        Mensaje = mensaje
    };

    public static ResultadoRestablecerContrasena CrearExito(string mensaje) => new()
    {
        Exito = true,
        Mensaje = mensaje
    };
}
