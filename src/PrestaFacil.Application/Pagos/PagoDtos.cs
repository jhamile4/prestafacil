using FluentValidation;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Pagos;

// ---------- Peticiones ----------

/// <param name="NumeroOperacion">Obligatorio para Transferencia, Yape y Plin.</param>
public record RegistrarPagoRequest(decimal Monto, MedioPago MedioPago, string? NumeroOperacion = null);

public record AnularPagoRequest(string Motivo);

public class RegistrarPagoValidator : AbstractValidator<RegistrarPagoRequest>
{
    public RegistrarPagoValidator()
    {
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
            .PrecisionScale(12, 2, true).WithMessage("El monto admite como máximo 2 decimales.");
        RuleFor(x => x.MedioPago).IsInEnum();
        RuleFor(x => x.NumeroOperacion)
            .NotEmpty().WithMessage("El número de operación es obligatorio para pagos digitales.")
            .When(x => x.MedioPago != MedioPago.Efectivo);
        RuleFor(x => x.NumeroOperacion).MaximumLength(50);
    }
}

public class AnularPagoValidator : AbstractValidator<AnularPagoRequest>
{
    public AnularPagoValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Debe indicar el motivo de la anulación.")
            .MaximumLength(300);
    }
}

// ---------- Respuestas ----------

public record PagoDetalleResponse(int NumeroCuota, decimal Mora, decimal Interes, decimal Capital, decimal Total);

public record PagoResponse(
    int Id,
    string Prestamo,
    DateTime FechaPago,
    decimal Monto,
    MedioPago MedioPago,
    string? NumeroOperacion,
    int CajeroId,
    bool Anulado,
    string? MotivoAnulacion,
    DateTime? FechaAnulacion,
    decimal TotalMora,
    decimal TotalInteres,
    decimal TotalCapital,
    IReadOnlyList<PagoDetalleResponse> Detalles)
{
    public static PagoResponse Desde(Pago p, string codigoPrestamo) => new(
        p.Id, codigoPrestamo, p.FechaPago, p.Monto, p.MedioPago, p.NumeroOperacion, p.CajeroId,
        p.Anulado, p.MotivoAnulacion, p.FechaAnulacion,
        p.Detalles.Sum(d => d.MontoMora), p.Detalles.Sum(d => d.MontoInteres), p.Detalles.Sum(d => d.MontoCapital),
        p.Detalles.OrderBy(d => d.Cuota.NumeroCuota)
            .Select(d => new PagoDetalleResponse(d.Cuota.NumeroCuota, d.MontoMora, d.MontoInteres, d.MontoCapital, d.Total))
            .ToList());
}

/// <summary>Resultado de cobrar: el pago y cómo queda el préstamo.</summary>
public record PagoRegistradoResponse(PagoResponse Pago, EstadoPrestamo EstadoPrestamo, DeudaResponse DeudaRestante);

public record ProximaCuotaResponse(int Numero, DateOnly FechaVencimiento, decimal Pendiente);

public record DeudaResponse(
    string Prestamo,
    EstadoPrestamo Estado,
    DateOnly FechaCorte,
    int CuotasVencidas,
    int DiasAtrasoMaximo,
    decimal SaldoVencido,
    decimal MoraPendiente,
    decimal TotalParaPonerseAlDia,
    decimal DeudaTotal,
    ProximaCuotaResponse? ProximaCuota)
{
    public static DeudaResponse Desde(Prestamo p, ResumenDeuda d) => new(
        p.Codigo, p.Estado, d.FechaCorte, d.CuotasVencidas, d.DiasAtrasoMaximo, d.SaldoVencido,
        d.MoraPendiente, d.TotalParaPonerseAlDia, d.DeudaTotal,
        d.ProximaCuota is { } c ? new ProximaCuotaResponse(c.NumeroCuota, c.FechaVencimiento, c.Pendiente) : null);
}
