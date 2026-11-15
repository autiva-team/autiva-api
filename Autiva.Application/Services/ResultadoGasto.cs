namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de registrar un gasto de otra categoría.
/// </summary>
public class ResultadoGasto
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Conflicto { get; init; }

    public string? Mensaje { get; init; }

    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();

    public GastoResponse? Datos { get; init; }

    public static ResultadoGasto CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoGasto CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoGasto CrearValidacion(IReadOnlyList<string> errores) => new()
    {
        Exito = false,
        Errores = errores
    };

    public static ResultadoGasto CrearConflicto(string mensaje) => new()
    {
        Exito = false,
        Conflicto = true,
        Mensaje = mensaje
    };

    public static ResultadoGasto CrearExito(GastoResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
