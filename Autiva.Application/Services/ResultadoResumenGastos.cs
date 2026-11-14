namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar el resumen de gastos del mes de un vehículo del usuario autenticado.
/// </summary>
public class ResultadoResumenGastos
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Validacion { get; init; }

    public string? Mensaje { get; init; }

    public ResumenGastosResponse? Datos { get; init; }

    public static ResultadoResumenGastos CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoResumenGastos CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoResumenGastos CrearValidacion(string mensaje) => new()
    {
        Exito = false,
        Validacion = true,
        Mensaje = mensaje
    };

    public static ResultadoResumenGastos CrearExito(ResumenGastosResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
