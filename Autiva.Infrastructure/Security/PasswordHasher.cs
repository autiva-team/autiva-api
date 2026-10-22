using Autiva.Application.Interfaces;

namespace Autiva.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string contrasena) => BCrypt.Net.BCrypt.HashPassword(contrasena);

    public bool Verify(string contrasena, string hash) => BCrypt.Net.BCrypt.Verify(contrasena, hash);
}
