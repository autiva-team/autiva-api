namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de listar los vehículos del usuario autenticado.
/// </summary>
public class ResultadoListaVehiculos
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public string? Mensaje { get; init; }

    public IReadOnlyList<VehiculoResumenDto> Datos { get; init; } = Array.Empty<VehiculoResumenDto>();

    public static ResultadoListaVehiculos CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoListaVehiculos CrearExito(IReadOnlyList<VehiculoResumenDto> datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
