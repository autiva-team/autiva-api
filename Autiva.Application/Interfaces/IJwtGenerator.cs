using Autiva.Domain.Entities;

namespace Autiva.Application.Interfaces;

/// <summary>
/// Generación de tokens JWT para una cuenta autenticada.
/// </summary>
public interface IJwtGenerator
{
    (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario);
}
