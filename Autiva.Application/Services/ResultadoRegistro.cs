namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de una operación de registro de cuenta.
/// </summary>
public class ResultadoRegistro
{
    public bool Exito { get; init; }

    public bool Conflicto { get; init; }

    public string? Mensaje { get; init; }

    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();

    public RegistroUsuarioResponse? Datos { get; init; }

    public static ResultadoRegistro CrearValidacion(IReadOnlyList<string> errores) => new()
    {
        Exito = false,
        Errores = errores
    };

    public static ResultadoRegistro CrearConflicto(string mensaje) => new()
    {
        Exito = false,
        Conflicto = true,
        Mensaje = mensaje
    };

    public static ResultadoRegistro CrearExito(RegistroUsuarioResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
