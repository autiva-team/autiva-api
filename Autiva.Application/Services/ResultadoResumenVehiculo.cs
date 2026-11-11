namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de obtener el resumen general de un vehículo.
/// </summary>
public class ResultadoResumenVehiculo
{
    public bool Exito { get; init; }
    public bool NoAutorizado { get; init; }
    public bool NoEncontrado { get; init; }
    public string? Mensaje { get; init; }
    public VehiculoResumenResponse? Datos { get; init; }

    public static ResultadoResumenVehiculo CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoResumenVehiculo CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoResumenVehiculo CrearExito(VehiculoResumenResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
