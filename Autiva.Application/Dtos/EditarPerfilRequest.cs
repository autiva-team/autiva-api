namespace Autiva.Application.Dtos;

/// <summary>
/// Datos enviados para editar el perfil. La contraseña no se cambia aquí (HU-027).
/// </summary>
public class EditarPerfilRequest
{
    public string Nombre { get; set; } = null!;
    public string Correo { get; set; } = null!;
}
