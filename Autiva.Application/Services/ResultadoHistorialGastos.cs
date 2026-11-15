namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar el historial paginado de gastos de un vehículo.
/// </summary>
public class ResultadoHistorialGastos
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Validacion { get; init; }

    public string? Mensaje { get; init; }

    public ResultadoPaginado<GastoResponse>? Datos { get; init; }

    public static ResultadoHistorialGastos CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoHistorialGastos CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoHistorialGastos CrearValidacion(string mensaje) => new()
    {
        Exito = false,
        Validacion = true,
        Mensaje = mensaje
    };

    public static ResultadoHistorialGastos CrearExito(ResultadoPaginado<GastoResponse> datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
