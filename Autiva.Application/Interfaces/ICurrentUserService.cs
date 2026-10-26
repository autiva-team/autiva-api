namespace Autiva.Application.Interfaces;

/// <summary>
/// Expone la identidad del usuario autenticado sin depender de ASP.NET Core.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Id del usuario del token, o <c>null</c> si no está autenticado.</summary>
    Guid? UsuarioId { get; }

    bool EstaAutenticado { get; }
}
