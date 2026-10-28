namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de obtener el detalle de un vehículo del usuario autenticado.
/// </summary>
public class ResultadoVehiculoDetalle
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public string? Mensaje { get; init; }

    public VehiculoResponse? Datos { get; init; }

    public static ResultadoVehiculoDetalle CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoVehiculoDetalle CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoVehiculoDetalle CrearExito(VehiculoResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
