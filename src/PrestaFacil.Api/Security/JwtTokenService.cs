using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PrestaFacil.Application.Usuarios;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Api.Security;

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider reloj) : ITokenService
{
    public const string ClaimRol = "role";
    public const string ClaimNombre = "name";

    private readonly JwtOptions _opciones = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenGenerado Generar(Usuario usuario)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(_opciones.ExpiracionMinutos);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _opciones.Issuer,
            Audience = _opciones.Audience,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = expira,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),
                new Claim(ClaimNombre, usuario.NombreCompleto),
                new Claim(ClaimRol, Roles.Nombre(usuario.Rol)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_opciones.KeyBytes), SecurityAlgorithms.HmacSha256)
        });

        return new TokenGenerado(token, expira);
    }
}
