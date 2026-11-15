namespace Autiva.Application.Services;

/// <summary>
/// Datos mínimos de un resultado de servicio que un controlador necesita para decidir
/// el código HTTP de error (401/404/409/400), sin acoplarse al DTO de datos de éxito.
/// Los <c>ResultadoX</c> que no tienen <see cref="Conflicto"/>, <see cref="Validacion"/>
/// o <see cref="Errores"/> (por ejemplo los de un simple borrado) usan el valor por
/// defecto de la interfaz, que nunca cambia el comportamiento actual porque esos
/// resultados nunca marcan esas banderas en primer lugar.
/// </summary>
public interface IResultadoOperacion
{
    bool Exito { get; }

    bool NoAutorizado { get; }

    bool NoEncontrado { get; }

    string? Mensaje { get; }

    bool Conflicto => false;

    bool Validacion => false;

    IReadOnlyList<string> Errores => Array.Empty<string>();
}
