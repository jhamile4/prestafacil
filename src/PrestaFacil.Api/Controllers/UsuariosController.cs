using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrestaFacil.Api.Security;
using PrestaFacil.Application.Usuarios;

namespace PrestaFacil.Api.Controllers;

/// <summary>Gestión de usuarios del sistema (RF-21). Solo ADMIN.</summary>
[ApiController]
[Route("api/usuarios")]
[Produces("application/json")]
[Authorize(Roles = Permisos.Admin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class UsuariosController(IUsuarioService usuarios) : ControllerBase
{
    /// <summary>Lista todos los usuarios.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UsuarioResponse>> Listar(CancellationToken ct) => usuarios.ListarAsync(ct);

    /// <summary>Obtiene un usuario.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UsuarioResponse> Obtener(int id, CancellationToken ct) => usuarios.ObtenerAsync(id, ct);

    /// <summary>Crea un usuario. `rol`: Admin, Asesor o Cajero.</summary>
    [HttpPost]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await usuarios.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = usuario.Id }, usuario);
    }

    /// <summary>Actualiza nombre, correo y rol.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<UsuarioResponse> Actualizar(int id, ActualizarUsuarioRequest request, CancellationToken ct) =>
        usuarios.ActualizarAsync(id, request, ct);

    /// <summary>Desactiva un usuario: ya no podrá iniciar sesión (RN-11).</summary>
    [HttpPatch("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await usuarios.DesactivarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Reactiva un usuario.</summary>
    [HttpPatch("{id:int}/activar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        await usuarios.ActivarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Asigna una nueva contraseña a un usuario que olvidó la suya.</summary>
    [HttpPost("{id:int}/restablecer-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RestablecerPassword(int id, RestablecerPasswordRequest request, CancellationToken ct)
    {
        await usuarios.RestablecerPasswordAsync(id, request, ct);
        return NoContent();
    }
}
