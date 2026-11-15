namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para guardar las preferencias de la app. UltimaSincronizacion no se
/// edita desde aquí.
/// </summary>
public class ActualizarPreferenciasRequest
{
    public bool NotificacionesPush { get; set; }
    public string UnidadMedida { get; set; } = null!;
    public bool RespaldoNube { get; set; }
    public bool ModoOffline { get; set; }
}
