namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de registrar un servicio de mantenimiento.
/// </summary>
public class ResultadoServicio
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Conflicto { get; init; }

    public string? Mensaje { get; init; }

    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();

    public ServicioResponse? Datos { get; init; }

    public static ResultadoServicio CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoServicio CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoServicio CrearValidacion(IReadOnlyList<string> errores) => new()
    {
        Exito = false,
        Errores = errores
    };

    public static ResultadoServicio CrearConflicto(string mensaje) => new()
    {
        Exito = false,
        Conflicto = true,
        Mensaje = mensaje
    };

    public static ResultadoServicio CrearExito(ServicioResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
