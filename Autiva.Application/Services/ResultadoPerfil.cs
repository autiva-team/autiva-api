namespace Autiva.Application.Services;

using Autiva.Application.Dtos;

/// <summary>
/// Resultado de consultar o editar el perfil del usuario autenticado.
/// </summary>
public class ResultadoPerfil : IResultadoOperacion
{
    public bool Exito { get; init; }

    public bool NoAutorizado { get; init; }

    public bool NoEncontrado { get; init; }

    public bool Conflicto { get; init; }

    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();

    public string? Mensaje { get; init; }

    public PerfilResponse? Datos { get; init; }

    public static ResultadoPerfil CrearNoAutorizado(string mensaje) => new()
    {
        Exito = false,
        NoAutorizado = true,
        Mensaje = mensaje
    };

    public static ResultadoPerfil CrearConflicto(string mensaje) => new()
    {
        Exito = false,
        Conflicto = true,
        Mensaje = mensaje
    };

    public static ResultadoPerfil CrearValidacion(IReadOnlyList<string> errores) => new()
    {
        Exito = false,
        Errores = errores
    };

    public static ResultadoPerfil CrearExito(PerfilResponse datos) => new()
    {
        Exito = true,
        Datos = datos
    };
}
