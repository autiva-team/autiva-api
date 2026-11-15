namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar el avance hacia el próximo servicio de un vehículo del usuario autenticado.
/// </summary>
public class ResultadoAvanceServicio
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public string? Mensaje { get; init; }

    public AvanceServicioResponse? Datos { get; init; }

    public static ResultadoAvanceServicio CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoAvanceServicio CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoAvanceServicio CrearExito(AvanceServicioResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
