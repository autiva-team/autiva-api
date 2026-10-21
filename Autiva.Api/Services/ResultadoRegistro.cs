namespace Autiva.Api.Services;

using Autiva.Api.Dtos;

/// <summary>
/// Resultado de una operación de registro de cuenta.
/// </summary>
public class ResultadoRegistro
{
    public bool Exito { get; init; }

    public bool Conflicto { get; init; }

    public string? Mensaje { get; init; }

    public RegistroUsuarioResponse? Datos { get; init; }

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
