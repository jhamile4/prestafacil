using PrestaFacil.Application.Common;

namespace PrestaFacil.Api.Services;

/// <summary>
/// TEMPORAL hasta la fase de autenticación: toma el usuario de appsettings
/// (Auditoria:UsuarioPorDefectoId). Se reemplazará por el usuario del token JWT.
/// </summary>
public class ConfigCurrentUser(IConfiguration configuration) : ICurrentUser
{
    public int UserId { get; } = configuration.GetValue("Auditoria:UsuarioPorDefectoId", 2);
}
