namespace PrestaFacil.Domain.Entities;

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

    public decimal MontoCuota => Capital + Interes;
    public decimal TotalPagado => CapitalPagado + InteresPagado + MoraPagada;
    public decimal Pendiente => Capital + Interes + Mora - TotalPagado;

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
}
