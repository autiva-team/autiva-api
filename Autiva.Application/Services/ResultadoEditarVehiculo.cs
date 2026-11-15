namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de editar los datos y el estatus de un vehículo.
/// </summary>
public class ResultadoEditarVehiculo
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Conflicto { get; init; }

    public string? Mensaje { get; init; }

    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();

    public VehiculoResponse? Datos { get; init; }

    public static ResultadoEditarVehiculo CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoEditarVehiculo CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoEditarVehiculo CrearValidacion(IReadOnlyList<string> errores) => new()
    {
        Exito = false,
        Errores = errores
    };

    public static ResultadoEditarVehiculo CrearConflicto(string mensaje) => new()
    {
        Exito = false,
        Conflicto = true,
        Mensaje = mensaje
    };

    public static ResultadoEditarVehiculo CrearExito(VehiculoResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
