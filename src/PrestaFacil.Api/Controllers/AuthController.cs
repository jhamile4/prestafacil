using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PrestaFacil.Application.Usuarios;

namespace PrestaFacil.Api.Controllers;

/// <summary>Inicio de sesión y datos del usuario autenticado (RF-19).</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Inicia sesión y devuelve un token JWT.</summary>
    /// <remarks>
    /// Copie el `token` de la respuesta y péguelo en el botón **Authorize** de Swagger.
    /// Máximo 5 intentos por minuto desde la misma IP.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesLogin.Politica)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken ct) =>
        auth.LoginAsync(request, ct);

    /// <summary>Datos del usuario dueño del token.</summary>
    [HttpGet("yo")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<UsuarioResponse> Yo(CancellationToken ct) =>
        auth.ObtenerUsuarioActualAsync(ct);

    /// <summary>Nombre y rol de cada usuario, para mostrar quién realizó cada operación.</summary>
    [HttpGet("directorio")]
    [ProducesResponseType<IReadOnlyList<UsuarioDirectorioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IReadOnlyList<UsuarioDirectorioResponse>> Directorio(CancellationToken ct) =>
        auth.DirectorioAsync(ct);

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    [HttpPost("cambiar-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest request, CancellationToken ct)
    {
        await auth.CambiarPasswordAsync(request, ct);
        return NoContent();
    }
}

public static class LimitesLogin
{
    public const string Politica = "login";
    public const int IntentosPorMinuto = 5;
}
