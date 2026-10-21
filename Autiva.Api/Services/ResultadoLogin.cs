namespace Autiva.Api.Services;

using Autiva.Api.Dtos;

/// <summary>
/// Resultado de un intento de inicio de sesión.
/// </summary>
public class ResultadoLogin
{
    public bool Exito { get; init; }

    public string? Mensaje { get; init; }

    public LoginResponse? Datos { get; init; }

    public static ResultadoLogin Fallo(string mensaje) => new()
    {
        Exito = false,
        Mensaje = mensaje
    };

    public static ResultadoLogin CrearExito(LoginResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
