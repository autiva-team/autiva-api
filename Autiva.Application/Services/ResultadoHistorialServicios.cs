namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar el historial paginado de servicios de un vehículo.
/// </summary>
public class ResultadoHistorialServicios
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public string? Mensaje { get; init; }

    public ResultadoPaginado<ServicioResponse>? Datos { get; init; }

    public static ResultadoHistorialServicios CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoHistorialServicios CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoHistorialServicios CrearExito(ResultadoPaginado<ServicioResponse> datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
