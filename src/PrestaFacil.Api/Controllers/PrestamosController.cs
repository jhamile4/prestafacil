using Microsoft.AspNetCore.Mvc;
using PrestaFacil.Application.Common;
using PrestaFacil.Application.Prestamos;

namespace PrestaFacil.Api.Controllers;

/// <summary>Solicitudes, evaluación, aprobación, desembolso y cronograma (RF-06 a RF-11).</summary>
[ApiController]
[Route("api/prestamos")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
public class PrestamosController(IPrestamoService prestamos) : ControllerBase
{
    /// <summary>Simula cuota y cronograma sin guardar nada (RF-07).</summary>
    /// <remarks>Si se envía `clienteId`, incluye la evaluación crediticia del cliente.</remarks>
    [HttpPost("simular")]
    [ProducesResponseType<SimulacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<SimulacionResponse> Simular(SimularPrestamoRequest request, CancellationToken ct) =>
        prestamos.SimularAsync(request, ct);

    /// <summary>Registra una solicitud de préstamo (RF-06) y devuelve su evaluación (RF-08).</summary>
    [HttpPost]
    [ProducesResponseType<PrestamoDetalleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PrestamoDetalleResponse>> Solicitar(SolicitarPrestamoRequest request, CancellationToken ct)
    {
        var prestamo = await prestamos.SolicitarAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = prestamo.Prestamo.Id }, prestamo);
    }

    /// <summary>Lista préstamos, filtrando por estado o cliente.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<PrestamoResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<PrestamoResponse>> Buscar([FromQuery] BuscarPrestamosQuery query, CancellationToken ct) =>
        prestamos.BuscarAsync(query, ct);

    /// <summary>Detalle del préstamo con su historial de estados.</summary>
    /// <remarks>Si está PENDIENTE, incluye la evaluación a la fecha.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PrestamoDetalleResponse>(StatusCodes.Status200OK)]
    public Task<PrestamoDetalleResponse> Obtener(int id, CancellationToken ct) =>
        prestamos.ObtenerAsync(id, ct);

    /// <summary>Evalúa la solicitud con las reglas RN-04 a RN-07 (RF-08).</summary>
    [HttpGet("{id:int}/evaluacion")]
    [ProducesResponseType<EvaluacionResponse>(StatusCodes.Status200OK)]
    public Task<EvaluacionResponse> Evaluar(int id, CancellationToken ct) =>
        prestamos.EvaluarAsync(id, ct);

    /// <summary>Aprueba la solicitud (RF-09). Vuelve a evaluar antes de aprobar.</summary>
    [HttpPost("{id:int}/aprobar")]
    [ProducesResponseType<PrestamoDetalleResponse>(StatusCodes.Status200OK)]
    public Task<PrestamoDetalleResponse> Aprobar(int id, AprobarPrestamoRequest request, CancellationToken ct) =>
        prestamos.AprobarAsync(id, request, ct);

    /// <summary>Rechaza la solicitud indicando el motivo (RF-09).</summary>
    [HttpPost("{id:int}/rechazar")]
    [ProducesResponseType<PrestamoDetalleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PrestamoDetalleResponse> Rechazar(int id, RechazarPrestamoRequest request, CancellationToken ct) =>
        prestamos.RechazarAsync(id, request, ct);

    /// <summary>Desembolsa un préstamo aprobado y genera su cronograma (RF-10).</summary>
    [HttpPost("{id:int}/desembolsar")]
    [ProducesResponseType<CronogramaResponse>(StatusCodes.Status200OK)]
    public Task<CronogramaResponse> Desembolsar(int id, DesembolsarPrestamoRequest request, CancellationToken ct) =>
        prestamos.DesembolsarAsync(id, request, ct);

    /// <summary>Cronograma de cuotas con lo pagado y lo pendiente (RF-11).</summary>
    [HttpGet("{id:int}/cronograma")]
    [ProducesResponseType<CronogramaResponse>(StatusCodes.Status200OK)]
    public Task<CronogramaResponse> Cronograma(int id, CancellationToken ct) =>
        prestamos.ObtenerCronogramaAsync(id, ct);
}
