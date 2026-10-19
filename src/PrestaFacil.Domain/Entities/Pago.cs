using PrestaFacil.Domain.Common;

namespace PrestaFacil.Domain.Entities;

public enum MedioPago
{
    Efectivo,
    Transferencia,
    Yape,
    Plin
}

public class Pago
{
    private readonly List<PagoDetalle> _detalles = [];

    public int Id { get; private set; }
    public int PrestamoId { get; private set; }
    public DateTime FechaPago { get; private set; }
    public decimal Monto { get; private set; }
    public MedioPago MedioPago { get; private set; }
    public string? NumeroOperacion { get; private set; }
    public int CajeroId { get; private set; }
    public bool Anulado { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public int? AnuladoPorId { get; private set; }
    public DateTime? FechaAnulacion { get; private set; }

    public IReadOnlyList<PagoDetalle> Detalles => _detalles;

    private Pago() { }

    internal Pago(decimal monto, MedioPago medioPago, string? numeroOperacion, int cajeroId, DateTime fechaPago)
    {
        if (monto <= 0)
            throw new DomainException("El monto del pago debe ser mayor a 0.");
        if (decimal.Round(monto, 2) != monto)
            throw new DomainException("El monto admite como máximo 2 decimales.");

        numeroOperacion = string.IsNullOrWhiteSpace(numeroOperacion) ? null : numeroOperacion.Trim();
        if (medioPago != MedioPago.Efectivo && numeroOperacion is null)
            throw new DomainException($"Los pagos por {medioPago} requieren el número de operación.");

        Monto = monto;
        MedioPago = medioPago;
        NumeroOperacion = numeroOperacion;
        CajeroId = cajeroId;
        FechaPago = fechaPago;
    }

    internal void AgregarDetalle(Cuota cuota, decimal mora, decimal interes, decimal capital) =>
        _detalles.Add(new PagoDetalle(cuota, mora, interes, capital));

    internal void Anular(int adminId, string motivo, DateTime ahora)
    {
        Anulado = true;
        MotivoAnulacion = motivo;
        AnuladoPorId = adminId;
        FechaAnulacion = ahora;
    }
}

/// <summary>Cuánto de un pago se aplicó a cada cuota y a qué concepto (RN-09).</summary>
public class PagoDetalle
{
    public int Id { get; private set; }
    public int PagoId { get; private set; }
    public int CuotaId { get; private set; }
    public Cuota Cuota { get; private set; } = null!;
    public decimal MontoMora { get; private set; }
    public decimal MontoInteres { get; private set; }
    public decimal MontoCapital { get; private set; }

    public decimal Total => MontoMora + MontoInteres + MontoCapital;

    private PagoDetalle() { }

    internal PagoDetalle(Cuota cuota, decimal mora, decimal interes, decimal capital)
    {
        Cuota = cuota;
        MontoMora = mora;
        MontoInteres = interes;
        MontoCapital = capital;
    }
}
