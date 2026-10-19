using PrestaFacil.Domain.Entities;
using PrestaFacil.Domain.Services;

namespace PrestaFacil.Application.Prestamos;

// ---------- Peticiones ----------

/// <param name="ClienteId">Opcional: si se envía, la simulación incluye la evaluación del cliente.</param>
/// <param name="FechaDesembolso">Opcional: por defecto hoy.</param>
public record SimularPrestamoRequest(
    int ProductoId,
    decimal Monto,
    int PlazoMeses,
    int? ClienteId = null,
    DateOnly? FechaDesembolso = null);

public record SolicitarPrestamoRequest(int ClienteId, int ProductoId, decimal Monto, int PlazoMeses);

public record AprobarPrestamoRequest(string? Comentario = null);

public record RechazarPrestamoRequest(string Motivo);

/// <param name="FechaDesembolso">Opcional: por defecto hoy.</param>
public record DesembolsarPrestamoRequest(DateOnly? FechaDesembolso = null);

public record BuscarPrestamosQuery(
    EstadoPrestamo? Estado = null,
    int? ClienteId = null,
    int Pagina = 1,
    int TamanoPagina = 20);

// ---------- Respuestas ----------

public record ReglaResponse(string Codigo, string Descripcion, bool Cumple, string Detalle);

public record EvaluacionResponse(
    bool Apto,
    decimal CuotaNueva,
    decimal IngresoNeto,
    decimal LimiteCuotas,
    IReadOnlyList<ReglaResponse> Reglas)
{
    public static EvaluacionResponse Desde(ResultadoEvaluacion r) => new(
        r.Apto, r.CuotaNueva, r.IngresoNeto, r.LimiteCuotas,
        r.Reglas.Select(x => new ReglaResponse(x.Codigo, x.Descripcion, x.Cumple, x.Detalle)).ToList());
}

public record CuotaSimuladaResponse(
    int Numero,
    DateOnly FechaVencimiento,
    decimal SaldoInicial,
    decimal Capital,
    decimal Interes,
    decimal Cuota,
    decimal SaldoFinal);

public record SimulacionResponse(
    string Producto,
    decimal Monto,
    int PlazoMeses,
    decimal Tea,
    decimal Tem,
    decimal CuotaMensual,
    decimal TotalIntereses,
    decimal TotalAPagar,
    IReadOnlyList<CuotaSimuladaResponse> Cronograma,
    EvaluacionResponse? Evaluacion);

public record PrestamoResponse(
    int Id,
    string Codigo,
    int ClienteId,
    string ClienteDni,
    string Cliente,
    string Producto,
    decimal Monto,
    int PlazoMeses,
    decimal Tea,
    decimal CuotaMensual,
    EstadoPrestamo Estado,
    DateTime FechaSolicitud,
    DateTime? FechaAprobacion,
    DateOnly? FechaDesembolso,
    string? MotivoRechazo)
{
    public static PrestamoResponse Desde(Prestamo p) => new(
        p.Id, p.Codigo, p.ClienteId, p.Cliente.Dni, $"{p.Cliente.Apellidos}, {p.Cliente.Nombres}",
        p.Producto.Nombre, p.Monto, p.PlazoMeses, p.Tea, p.CuotaMensual, p.Estado,
        p.FechaSolicitud, p.FechaAprobacion, p.FechaDesembolso, p.MotivoRechazo);
}

public record HistorialResponse(
    EstadoPrestamo? EstadoAnterior,
    EstadoPrestamo EstadoNuevo,
    int UsuarioId,
    DateTime Fecha,
    string? Comentario);

public record PrestamoDetalleResponse(
    PrestamoResponse Prestamo,
    decimal Tem,
    decimal TasaMoraDiaria,
    int AsesorId,
    int? AprobadoPorId,
    int? DesembolsadoPorId,
    IReadOnlyList<HistorialResponse> Historial,
    EvaluacionResponse? Evaluacion); // Solo en préstamos PENDIENTES: evaluación a la fecha

public record CuotaResponse(
    int Numero,
    DateOnly FechaVencimiento,
    decimal SaldoInicial,
    decimal Capital,
    decimal Interes,
    decimal MontoCuota,
    decimal Mora,
    decimal TotalPagado,
    decimal Pendiente,
    EstadoCuota Estado,
    DateOnly? FechaPagoTotal)
{
    public static CuotaResponse Desde(Cuota c) => new(
        c.NumeroCuota, c.FechaVencimiento, c.SaldoInicial, c.Capital, c.Interes, c.MontoCuota,
        c.Mora, c.TotalPagado, c.Pendiente, c.Estado, c.FechaPagoTotal);
}

public record CronogramaResponse(
    string Codigo,
    EstadoPrestamo Estado,
    decimal Monto,
    decimal CuotaMensual,
    decimal TotalIntereses,
    decimal TotalPagado,
    decimal DeudaPendiente,
    IReadOnlyList<CuotaResponse> Cuotas);
