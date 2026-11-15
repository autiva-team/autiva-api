namespace Autiva.Application.Services;

/// <summary>
/// Resultado de eliminar (borrado lógico) una carga de combustible.
/// </summary>
public class ResultadoEliminarCarga
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public string? Mensaje { get; init; }

    public static ResultadoEliminarCarga CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoEliminarCarga CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoEliminarCarga CrearExito() => new()
    {
        Exito = true
    };
}
