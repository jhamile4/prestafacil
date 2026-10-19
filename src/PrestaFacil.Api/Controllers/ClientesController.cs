using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrestaFacil.Api.Security;
using PrestaFacil.Application.Clientes;
using PrestaFacil.Application.Common;

namespace PrestaFacil.Api.Controllers;

/// <summary>Gestión de clientes (RF-01 a RF-04).</summary>
[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ClientesController(IClienteService clientes) : ControllerBase
{
    /// <summary>Busca clientes por DNI o por nombre, con paginación.</summary>
    /// <remarks>
    /// `buscar` acepta un DNI (o sus primeros dígitos) o parte del nombre;
    /// no distingue tildes ni mayúsculas.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<PagedResult<ClienteResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<ClienteResponse>> Buscar([FromQuery] BuscarClientesQuery query, CancellationToken ct) =>
        clientes.BuscarAsync(query, ct);

    /// <summary>Obtiene un cliente por su id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ClienteResponse> ObtenerPorId(int id, CancellationToken ct) =>
        clientes.ObtenerPorIdAsync(id, ct);

    /// <summary>Obtiene un cliente por su DNI.</summary>
    [HttpGet("dni/{dni}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ClienteResponse> ObtenerPorDni(string dni, CancellationToken ct) =>
        clientes.ObtenerPorDniAsync(dni, ct);

    /// <summary>Registra un nuevo cliente.</summary>
    [HttpPost]
    [Authorize(Roles = Permisos.AdminAsesor)]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClienteResponse>> Crear(CrearClienteRequest request, CancellationToken ct)
    {
        var cliente = await clientes.CrearAsync(request, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.Id }, cliente);
    }

    /// <summary>Actualiza los datos de un cliente. El DNI no se puede cambiar.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Permisos.AdminAsesor)]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ClienteResponse> Actualizar(int id, ActualizarClienteRequest request, CancellationToken ct) =>
        clientes.ActualizarAsync(id, request, ct);

    /// <summary>Desactiva un cliente. No se elimina para conservar su historial (RN-11).</summary>
    [HttpPatch("{id:int}/desactivar")]
    [Authorize(Roles = Permisos.AdminAsesor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await clientes.DesactivarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Vuelve a activar un cliente desactivado.</summary>
    [HttpPatch("{id:int}/activar")]
    [Authorize(Roles = Permisos.AdminAsesor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        await clientes.ActivarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Estado de cuenta: préstamos del cliente, cuotas por pagar y deuda total.</summary>
    [HttpGet("{id:int}/estado-cuenta")]
    [ProducesResponseType<EstadoCuentaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<EstadoCuentaResponse> EstadoCuenta(int id, CancellationToken ct) =>
        clientes.ObtenerEstadoCuentaAsync(id, ct);
}
