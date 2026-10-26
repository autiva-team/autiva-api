namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de una operación de registro de vehículo.
/// </summary>
public class ResultadoVehiculo
{
    public bool Exito { get; init; }

    public bool Conflicto { get; init; }

    public bool NoAutorizado { get; init; }

    public string? Mensaje { get; init; }

    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();

    public VehiculoResponse? Datos { get; init; }

    public static ResultadoVehiculo CrearValidacion(IReadOnlyList<string> errores) => new()
    {
        Exito = false,
        Errores = errores
    };

    public static ResultadoVehiculo CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoVehiculo CrearConflicto(string mensaje) => new()
    {
        Exito = false,
        Conflicto = true,
        Mensaje = mensaje
    };

    public static ResultadoVehiculo CrearExito(VehiculoResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
