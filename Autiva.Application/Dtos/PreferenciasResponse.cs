namespace Autiva.Application.Dtos;

/// <summary>
/// Preferencias de la app del usuario autenticado.
/// </summary>
public class PreferenciasResponse
{
    public bool NotificacionesPush { get; set; }
    public string UnidadMedida { get; set; } = null!;
    public bool RespaldoNube { get; set; }
    public bool ModoOffline { get; set; }
    public DateTime? UltimaSincronizacion { get; set; }
}
