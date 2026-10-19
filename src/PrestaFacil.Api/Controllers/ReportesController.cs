using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrestaFacil.Api.Security;
using PrestaFacil.Application.Reportes;

namespace PrestaFacil.Api.Controllers;

/// <summary>Reportes (RF-16 a RF-18) y proceso de mora (RF-14), sobre procedimientos almacenados.</summary>
[ApiController]
[Route("api/reportes")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ReportesController(IReporteService reportes) : ControllerBase
{
    /// <summary>Cartera: préstamos, monto y saldo de capital por estado (sp_ReporteCartera).</summary>
    [HttpGet("cartera")]
    [Authorize(Roles = Permisos.Admin)]
    [ProducesResponseType<IReadOnlyList<CarteraItem>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CarteraItem>> Cartera(CancellationToken ct) =>
        reportes.CarteraAsync(ct);

    /// <summary>Morosos: préstamos con cuotas vencidas hace al menos `diasMinimos` días (sp_ReporteMorosos).</summary>
    [HttpGet("morosos")]
    [Authorize(Roles = Permisos.AdminAsesor)]
    [ProducesResponseType<IReadOnlyList<MorosoItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<MorosoItem>> Morosos([FromQuery] int diasMinimos = 1, CancellationToken ct = default) =>
        reportes.MorososAsync(diasMinimos, ct);

    /// <summary>Cobranza por cajero y medio de pago entre dos fechas (sp_ReporteCobranza).</summary>
    [HttpGet("cobranza")]
    [Authorize(Roles = Permisos.Admin)]
    [ProducesResponseType<IReadOnlyList<CobranzaItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<CobranzaItem>> Cobranza(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken ct) =>
        reportes.CobranzaAsync(desde, hasta, ct);

    /// <summary>Ejecuta ahora el proceso diario de mora (sp_ActualizarCuotasVencidas).</summary>
    /// <remarks>También corre solo cada día en segundo plano (ver Mora:ProcesoAutomatico).</remarks>
    [HttpPost("~/api/mora/procesar")]
    [Authorize(Roles = Permisos.Admin)]
    [ProducesResponseType<ProcesoMoraResponse>(StatusCodes.Status200OK)]
    public Task<ProcesoMoraResponse> ProcesarMora(CancellationToken ct) =>
        reportes.ProcesarMoraAsync(ct);
}
