namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar el historial paginado de cargas de un vehículo.
/// </summary>
public class ResultadoHistorialCargas
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Validacion { get; init; }

    public string? Mensaje { get; init; }

    public ResultadoPaginado<CargaResponse>? Datos { get; init; }

    public static ResultadoHistorialCargas CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoHistorialCargas CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoHistorialCargas CrearValidacion(string mensaje) => new()
    {
        Exito = false,
        Validacion = true,
        Mensaje = mensaje
    };

    public static ResultadoHistorialCargas CrearExito(ResultadoPaginado<CargaResponse> datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
