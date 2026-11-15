namespace Autiva.Application.Services;

/// <summary>
/// Resultado de eliminar (borrado lógico) un vehículo del usuario autenticado.
/// </summary>
public class ResultadoEliminarVehiculo
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public string? Mensaje { get; init; }

    public static ResultadoEliminarVehiculo CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoEliminarVehiculo CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoEliminarVehiculo CrearExito() => new()
    {
        Exito = true
    };
}
