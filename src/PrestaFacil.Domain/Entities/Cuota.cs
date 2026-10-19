using PrestaFacil.Domain.Services;

namespace PrestaFacil.Domain.Entities;

/// <summary>Montos aplicados a una cuota por concepto.</summary>
public readonly record struct Aplicacion(decimal Mora, decimal Interes, decimal Capital)
{
    public decimal Total => Mora + Interes + Capital;
}

public class Cuota
{
    public int Id { get; private set; }
    public int PrestamoId { get; private set; }
    public int NumeroCuota { get; private set; }
    public DateOnly FechaVencimiento { get; private set; }
    public decimal SaldoInicial { get; private set; }
    public decimal Capital { get; private set; }
    public decimal Interes { get; private set; }
    public decimal Mora { get; private set; }
    public decimal CapitalPagado { get; private set; }
    public decimal InteresPagado { get; private set; }
    public decimal MoraPagada { get; private set; }
    public EstadoCuota Estado { get; private set; }
    public DateOnly? FechaPagoTotal { get; private set; }
    /// <summary>Token de concurrencia (ROWVERSION en SQL Server).</summary>
    public byte[] RowVersion { get; private set; } = [];

    public decimal MontoCuota => Capital + Interes;
    public decimal TotalPagado => CapitalPagado + InteresPagado + MoraPagada;
    public decimal Pendiente => Capital + Interes + Mora - TotalPagado;
    /// <summary>Capital e interés sin pagar: la base sobre la que se calcula la mora.</summary>
    public decimal SaldoVencible => Capital + Interes - CapitalPagado - InteresPagado;
    public decimal MoraPendiente => Mora - MoraPagada;

    private Cuota() { }

    internal Cuota(int numeroCuota, DateOnly fechaVencimiento, decimal saldoInicial, decimal capital, decimal interes)
    {
        NumeroCuota = numeroCuota;
        FechaVencimiento = fechaVencimiento;
        SaldoInicial = saldoInicial;
        Capital = capital;
        Interes = interes;
        Estado = EstadoCuota.Pendiente;
    }

    public int DiasAtraso(DateOnly fecha) =>
        Estado != EstadoCuota.Pagada && fecha > FechaVencimiento ? fecha.DayNumber - FechaVencimiento.DayNumber : 0;

    /// <summary>
    /// RN-08: mora = saldo vencido × tasa diaria × días de atraso.
    /// Misma fórmula que sp_ActualizarCuotasVencidas. Nunca baja de lo ya pagado.
    /// </summary>
    internal void ActualizarMora(DateOnly fechaCorte, decimal tasaMoraDiaria)
    {
        if (Estado == EstadoCuota.Pagada || FechaVencimiento >= fechaCorte || SaldoVencible <= 0)
            return;

        var calculada = CalculadoraPrestamo.Redondear(SaldoVencible * tasaMoraDiaria * DiasAtraso(fechaCorte));
        Mora = Math.Max(MoraPagada, calculada);
        Estado = EstadoCuota.Vencida;
    }

    /// <summary>RN-09: dentro de la cuota se paga primero la mora, luego el interés y al final el capital.</summary>
    internal Aplicacion Aplicar(decimal disponible, DateOnly fecha)
    {
        var mora = Math.Min(disponible, MoraPendiente);
        disponible -= mora;
        var interes = Math.Min(disponible, Interes - InteresPagado);
        disponible -= interes;
        var capital = Math.Min(disponible, Capital - CapitalPagado);

        MoraPagada += mora;
        InteresPagado += interes;
        CapitalPagado += capital;
        RecalcularEstado(fecha);

        return new Aplicacion(mora, interes, capital);
    }

    /// <summary>Deshace lo aplicado por un pago anulado.</summary>
    internal void Revertir(PagoDetalle detalle, DateOnly fecha)
    {
        MoraPagada -= detalle.MontoMora;
        InteresPagado -= detalle.MontoInteres;
        CapitalPagado -= detalle.MontoCapital;
        RecalcularEstado(fecha);
    }

    private void RecalcularEstado(DateOnly fecha)
    {
        if (Pendiente == 0)
        {
            Estado = EstadoCuota.Pagada;
            FechaPagoTotal ??= fecha;
            return;
        }

        FechaPagoTotal = null;
        Estado = FechaVencimiento < fecha ? EstadoCuota.Vencida
               : TotalPagado > 0 ? EstadoCuota.Parcial
               : EstadoCuota.Pendiente;
    }
}
