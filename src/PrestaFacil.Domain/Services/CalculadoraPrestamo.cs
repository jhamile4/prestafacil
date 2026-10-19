namespace PrestaFacil.Domain.Services;

/// <summary>Una fila del cronograma calculado, antes de guardarse como Cuota.</summary>
public record CuotaCalculada(
    int Numero,
    DateOnly FechaVencimiento,
    decimal SaldoInicial,
    decimal Capital,
    decimal Interes)
{
    public decimal MontoCuota => Capital + Interes;
    public decimal SaldoFinal => SaldoInicial - Capital;
}

/// <summary>
/// Sistema francés (cuota fija). Ver docs/01_requerimientos.md, sección 4.1.
/// Debe dar exactamente los mismos montos que database/03_seed.sql.
/// </summary>
public static class CalculadoraPrestamo
{
    /// <summary>Decimales con los que se guarda la TEM (columna DECIMAL(12,10)).</summary>
    public const int DecimalesTem = 10;

    /// <summary>TEM = (1 + TEA)^(1/12) − 1</summary>
    public static decimal CalcularTem(decimal tea)
    {
        if (tea <= 0) throw new ArgumentOutOfRangeException(nameof(tea), "La TEA debe ser mayor a 0.");

        // La raíz 12 no existe para decimal; se calcula en double y se redondea
        var tem = Math.Pow(1 + (double)tea, 1.0 / 12) - 1;
        return Math.Round((decimal)tem, DecimalesTem, MidpointRounding.AwayFromZero);
    }

    /// <summary>Cuota = P × TEM / (1 − (1 + TEM)^(−n))</summary>
    public static decimal CalcularCuota(decimal monto, decimal tem, int plazoMeses)
    {
        if (monto <= 0) throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser mayor a 0.");
        if (plazoMeses <= 0) throw new ArgumentOutOfRangeException(nameof(plazoMeses), "El plazo debe ser mayor a 0.");

        // (1 + TEM)^n con multiplicaciones en decimal para no perder precisión
        var factor = 1m;
        for (var i = 0; i < plazoMeses; i++) factor *= 1 + tem;

        var cuota = monto * tem / (1 - 1 / factor);
        return Redondear(cuota);
    }

    /// <summary>
    /// Genera el cronograma completo. La última cuota absorbe la diferencia
    /// de redondeo para que el saldo termine exactamente en 0.
    /// </summary>
    public static IReadOnlyList<CuotaCalculada> GenerarCronograma(
        decimal monto, decimal tem, int plazoMeses, decimal cuotaMensual, DateOnly fechaDesembolso)
    {
        var cuotas = new List<CuotaCalculada>(plazoMeses);
        var saldo = monto;

        for (var numero = 1; numero <= plazoMeses; numero++)
        {
            var interes = Redondear(saldo * tem);
            var capital = numero == plazoMeses ? saldo : cuotaMensual - interes;

            cuotas.Add(new CuotaCalculada(numero, fechaDesembolso.AddMonths(numero), saldo, capital, interes));
            saldo -= capital;
        }

        return cuotas;
    }

    /// <summary>Redondeo comercial a 2 decimales, igual que ROUND() de SQL Server.</summary>
    public static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
