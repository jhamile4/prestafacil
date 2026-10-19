using Microsoft.IdentityModel.JsonWebTokens;
using PrestaFacil.Application.Common;

namespace PrestaFacil.Api.Security;

/// <summary>Usuario autenticado, leído del claim "sub" del token JWT.</summary>
public class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public int UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(sub, out var id)
                ? id
                : throw new UnauthorizedException("La petición no tiene un usuario autenticado.");
        }
    }
}
