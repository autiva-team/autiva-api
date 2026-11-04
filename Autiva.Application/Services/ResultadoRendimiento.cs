namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de obtener el rendimiento de un vehículo del usuario autenticado.
/// </summary>
public class ResultadoRendimiento
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public string? Mensaje { get; init; }

    public RendimientoResponse? Datos { get; init; }

    public static ResultadoRendimiento CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoRendimiento CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoRendimiento CrearExito(RendimientoResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
