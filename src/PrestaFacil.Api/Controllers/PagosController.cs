using Microsoft.AspNetCore.Mvc;
using PrestaFacil.Application.Pagos;

namespace PrestaFacil.Api.Controllers;

/// <summary>Cobranza: deuda a la fecha, pagos y anulaciones (RF-12, RF-13, RN-08 a RN-11).</summary>
[ApiController]
[Route("api/prestamos/{prestamoId:int}")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
public class PagosController(IPagoService pagos) : ControllerBase
{
    /// <summary>Cuánto debe hoy: cuotas vencidas, mora al día, total para ponerse al día y para cancelar.</summary>
    [HttpGet("deuda")]
    [ProducesResponseType<DeudaResponse>(StatusCodes.Status200OK)]
    public Task<DeudaResponse> Deuda(int prestamoId, CancellationToken ct) =>
        pagos.ConsultarDeudaAsync(prestamoId, ct);

    /// <summary>Pagos del préstamo, del más reciente al más antiguo, con su distribución por cuota.</summary>
    [HttpGet("pagos")]
    [ProducesResponseType<IReadOnlyList<PagoResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<PagoResponse>> Listar(int prestamoId, CancellationToken ct) =>
        pagos.ListarAsync(prestamoId, ct);

    /// <summary>Registra un pago y lo aplica: cuota más antigua primero; mora, interés y capital (RN-09).</summary>
    /// <remarks>`medioPago`: Efectivo, Transferencia, Yape o Plin. Los digitales requieren `numeroOperacion`.</remarks>
    [HttpPost("pagos")]
    [ProducesResponseType<PagoRegistradoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PagoRegistradoResponse>> Registrar(
        int prestamoId, RegistrarPagoRequest request, CancellationToken ct)
    {
        var resultado = await pagos.RegistrarAsync(prestamoId, request, ct);
        return CreatedAtAction(nameof(Listar), new { prestamoId }, resultado);
    }

    /// <summary>Anula el último pago válido y revierte sus abonos (RN-11).</summary>
    [HttpPost("pagos/{pagoId:int}/anular")]
    [ProducesResponseType<PagoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<PagoResponse> Anular(int prestamoId, int pagoId, AnularPagoRequest request, CancellationToken ct) =>
        pagos.AnularAsync(prestamoId, pagoId, request, ct);
}
