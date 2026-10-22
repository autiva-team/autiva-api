namespace Autiva.Application.Interfaces;

/// <summary>
/// Hash y verificación de contraseñas.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string contrasena);

    bool Verify(string contrasena, string hash);
}
