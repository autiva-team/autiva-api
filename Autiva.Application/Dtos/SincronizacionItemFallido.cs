namespace Autiva.Application.Dtos;

/// <summary>
/// Registro offline que no se pudo sincronizar (dato inválido, vehículo ajeno, etc.).
/// El cliente debe conservarlo y corregirlo; no se reintenta solo.
/// </summary>
public class SincronizacionItemFallido
{
    public Guid Id { get; set; }

    /// <summary>"carga" o "servicio".</summary>
    public string Tipo { get; set; } = null!;

    public string Motivo { get; set; } = null!;
}
