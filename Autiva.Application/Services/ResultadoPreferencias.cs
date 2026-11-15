namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar o guardar las preferencias del usuario autenticado.
/// </summary>
public class ResultadoPreferencias : IResultadoOperacion
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Validacion { get; init; }

    public string? Mensaje { get; init; }

    public PreferenciasResponse? Datos { get; init; }

    public static ResultadoPreferencias CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoPreferencias CrearValidacion(string mensaje) => new()
    {
        Exito = false,
        Validacion = true,
        Mensaje = mensaje
    };

    public static ResultadoPreferencias CrearExito(PreferenciasResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
