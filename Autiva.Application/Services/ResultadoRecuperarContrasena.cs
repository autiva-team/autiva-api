namespace Autiva.Application.Services;

/// <summary>
/// Resultado de una solicitud de recuperación de contraseña.
/// </summary>
public class ResultadoRecuperarContrasena : IResultadoOperacion
{
    public bool Exito { get; init; }

    public bool NoAutorizado => false;

    public bool NoEncontrado => false;

    public bool Validacion { get; init; }

    public string? Mensaje { get; init; }

    public static ResultadoRecuperarContrasena CrearValidacion(string mensaje) => new()
    {
        Exito = false,
        Validacion = true,
        Mensaje = mensaje
    };

    public static ResultadoRecuperarContrasena CrearExito(string mensaje) => new()
    {
        Exito = true,
        Mensaje = mensaje
    };
}
