namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de crear un recordatorio.
/// </summary>
public class ResultadoRecordatorio
{
    public bool Exito { get; init; }
    public bool NoAutorizado { get; init; }
    public bool NoEncontrado { get; init; }
    public string? Mensaje { get; init; }
    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();
    public RecordatorioResponse? Datos { get; init; }

    public static ResultadoRecordatorio CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoRecordatorio CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoRecordatorio CrearValidacion(IReadOnlyList<string> errores) => new()
    {
        Exito = false,
        Errores = errores
    };

    public static ResultadoRecordatorio CrearExito(RecordatorioResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
