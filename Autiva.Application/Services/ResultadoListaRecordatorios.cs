namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar listas de recordatorios.
/// </summary>
public class ResultadoListaRecordatorios
{
    public bool Exito { get; init; }
    public bool NoAutorizado { get; init; }
    public bool NoEncontrado { get; init; }
    public string? Mensaje { get; init; }
    public IReadOnlyList<RecordatorioDetalleResponse> Datos { get; init; } = Array.Empty<RecordatorioDetalleResponse>();

    public static ResultadoListaRecordatorios CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoListaRecordatorios CrearNoEncontrado(string mensaje) => new()
    {
        Exito = false,
        NoEncontrado = true,
        Mensaje = mensaje
    };

    public static ResultadoListaRecordatorios CrearExito(IReadOnlyList<RecordatorioDetalleResponse> datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
