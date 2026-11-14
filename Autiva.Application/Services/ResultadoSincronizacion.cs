namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado del proceso de sincronización en lote.
/// </summary>
public class ResultadoSincronizacion
{
    public bool Exito { get; init; }
    public bool NoAutorizado { get; init; }
    public string? Mensaje { get; init; }
    public SincronizacionLoteResponse? Datos { get; init; }

    public static ResultadoSincronizacion CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoSincronizacion CrearExito(SincronizacionLoteResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
