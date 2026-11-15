namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar las estadísticas de un vehículo del usuario autenticado.
/// </summary>
public class ResultadoEstadisticas : IResultadoOperacion
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Validacion { get; init; }

    public string? Mensaje { get; init; }

    public EstadisticasResponse? Datos { get; init; }

    public static ResultadoEstadisticas CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoEstadisticas CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoEstadisticas CrearValidacion(string mensaje) => new()
    {
        Exito = false,
        Validacion = true,
        Mensaje = mensaje
    };

    public static ResultadoEstadisticas CrearExito(EstadisticasResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
