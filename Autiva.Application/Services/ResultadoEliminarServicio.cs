namespace Autiva.Application.Services;

/// <summary>
/// Resultado de eliminar (borrado lógico) un servicio de mantenimiento.
/// </summary>
public class ResultadoEliminarServicio
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public string? Mensaje { get; init; }

    public static ResultadoEliminarServicio CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoEliminarServicio CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoEliminarServicio CrearExito() => new()
    {
        Exito = true
    };
}
