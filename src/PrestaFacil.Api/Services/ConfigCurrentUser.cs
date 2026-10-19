using PrestaFacil.Application.Common;

namespace PrestaFacil.Api.Services;

/// <summary>
/// TEMPORAL hasta la fase de autenticación JWT.
/// En desarrollo, la cabecera <c>X-Usuario-Id</c> permite actuar como otro usuario
/// (1 = admin, 2 = asesor1, 3 = asesor2, 4 = cajero1). Sin cabecera se usa
/// Auditoria:UsuarioPorDefectoId de appsettings.
/// </summary>
public class ConfigCurrentUser(
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment) : ICurrentUser
{
    public const string CabeceraUsuario = "X-Usuario-Id";

    public int UserId
    {
        get
        {
            if (environment.IsDevelopment()
                && httpContextAccessor.HttpContext?.Request.Headers[CabeceraUsuario].ToString() is { Length: > 0 } valor
                && int.TryParse(valor, out var id))
                return id;

            return configuration.GetValue("Auditoria:UsuarioPorDefectoId", 2);
        }
    }
}
