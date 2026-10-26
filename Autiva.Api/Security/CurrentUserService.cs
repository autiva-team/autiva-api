using System.Security.Claims;
using Autiva.Application.Interfaces;

namespace Autiva.Api.Security;

/// <summary>
/// Implementación de <see cref="ICurrentUserService"/> basada en el HttpContext.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UsuarioId
    {
        get
        {
            var usuario = _httpContextAccessor.HttpContext?.User;

            var valor = usuario?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? usuario?.FindFirstValue("sub");

            return Guid.TryParse(valor, out var usuarioId) ? usuarioId : null;
        }
    }

    public bool EstaAutenticado =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
