namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

public class ResultadoPush
{
    public bool Exito { get; init; }
    public bool NoAutorizado { get; init; }
    public string? Mensaje { get; init; }
    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();
    public SuscripcionPushResponse? Datos { get; init; }

    public static ResultadoPush CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoPush CrearValidacion(IReadOnlyList<string> errores) => new()
    {
        Exito = false,
        Errores = errores
    };

    public static ResultadoPush CrearExito(SuscripcionPushResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
