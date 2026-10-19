using PrestaFacil.Domain.Services;

namespace PrestaFacil.UnitTests.Prestamos;

/// <summary>
/// Los valores esperados salen de database/03_seed.sql ejecutado en SQL Server:
/// la API en C# y la base de datos deben calcular exactamente lo mismo.
/// </summary>
public class CalculadoraPrestamoTests
{
    [Theory]
    [InlineData(0.35, 0.0253240572)]
    [InlineData(0.28, 0.0207847285)]
    [InlineData(0.45, 0.0314479891)]
    public void CalcularTem_DesdeTea(decimal tea, decimal temEsperada)
    {
        Assert.Equal(temEsperada, CalculadoraPrestamo.CalcularTem(tea));
    }

    [Theory]
    [InlineData(5000, 12, 0.35, 488.39)]   // PR-000001
    [InlineData(15000, 18, 0.28, 1007.45)] // PR-000002
    [InlineData(3000, 6, 0.35, 545.24)]    // PR-000003
    [InlineData(1500, 6, 0.45, 278.23)]    // PR-000004
    [InlineData(8000, 24, 0.35, 448.91)]   // PR-000005
    [InlineData(20000, 12, 0.28, 1900.32)] // PR-000006
    [InlineData(10000, 12, 0.35, 976.79)]  // PR-000007
    [InlineData(1000, 3, 0.45, 354.52)]    // PR-000008
    public void CalcularCuota_IgualQueSqlServer(decimal monto, int plazo, decimal tea, decimal cuotaEsperada)
    {
        var tem = CalculadoraPrestamo.CalcularTem(tea);

        Assert.Equal(cuotaEsperada, CalculadoraPrestamo.CalcularCuota(monto, tem, plazo));
    }

    [Fact]
    public void GenerarCronograma_PR000001_CoincideFilaPorFilaConSqlServer()
    {
        // Número, vencimiento, saldo inicial, capital, interés
        (int, string, decimal, decimal, decimal)[] esperado =
        [
            (1,  "2026-03-19", 5000.00m, 361.77m, 126.62m),
            (2,  "2026-04-19", 4638.23m, 370.93m, 117.46m),
            (3,  "2026-05-19", 4267.30m, 380.32m, 108.07m),
            (4,  "2026-06-19", 3886.98m, 389.96m,  98.43m),
            (5,  "2026-07-19", 3497.02m, 399.83m,  88.56m),
            (6,  "2026-08-19", 3097.19m, 409.96m,  78.43m),
            (7,  "2026-09-19", 2687.23m, 420.34m,  68.05m),
            (8,  "2026-10-19", 2266.89m, 430.98m,  57.41m),
            (9,  "2026-11-19", 1835.91m, 441.90m,  46.49m),
            (10, "2026-12-19", 1394.01m, 453.09m,  35.30m),
            (11, "2027-01-19",  940.92m, 464.56m,  23.83m),
            (12, "2027-02-19",  476.36m, 476.36m,  12.06m),
        ];

        var tem = CalculadoraPrestamo.CalcularTem(0.35m);
        var cuotas = CalculadoraPrestamo.GenerarCronograma(5000m, tem, 12, 488.39m, new DateOnly(2026, 2, 19));

        var obtenido = cuotas
            .Select(c => (c.Numero, c.FechaVencimiento.ToString("yyyy-MM-dd"), c.SaldoInicial, c.Capital, c.Interes))
            .ToArray();
        Assert.Equal(esperado, obtenido);
    }

    [Theory]
    [InlineData(5000, 12, 0.35)]
    [InlineData(15000, 18, 0.28)]
    [InlineData(1000, 3, 0.45)]
    [InlineData(30000, 24, 0.28)]
    [InlineData(500, 3, 0.45)]
    public void GenerarCronograma_ElCapitalSumaElMontoYElSaldoTerminaEnCero(decimal monto, int plazo, decimal tea)
    {
        var tem = CalculadoraPrestamo.CalcularTem(tea);
        var cuota = CalculadoraPrestamo.CalcularCuota(monto, tem, plazo);

        var cronograma = CalculadoraPrestamo.GenerarCronograma(monto, tem, plazo, cuota, new DateOnly(2026, 1, 15));

        Assert.Equal(plazo, cronograma.Count);
        Assert.Equal(monto, cronograma.Sum(c => c.Capital));
        Assert.Equal(0m, cronograma[^1].SaldoFinal);
        // Solo la última cuota puede diferir: absorbe el redondeo acumulado (máx. ~1 céntimo por mes)
        Assert.All(cronograma.SkipLast(1), c => Assert.Equal(cuota, c.MontoCuota));
        Assert.InRange(Math.Abs(cronograma[^1].MontoCuota - cuota), 0m, plazo * 0.01m);
    }

    [Fact]
    public void GenerarCronograma_DesembolsoAFinDeMes_VenceElUltimoDiaDeCadaMes()
    {
        var tem = CalculadoraPrestamo.CalcularTem(0.35m);

        var cronograma = CalculadoraPrestamo.GenerarCronograma(1000m, tem, 3, 350m, new DateOnly(2027, 1, 31));

        Assert.Equal(new DateOnly(2027, 2, 28), cronograma[0].FechaVencimiento);
        Assert.Equal(new DateOnly(2027, 3, 31), cronograma[1].FechaVencimiento);
        Assert.Equal(new DateOnly(2027, 4, 30), cronograma[2].FechaVencimiento);
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(-100, 12)]
    [InlineData(1000, 0)]
    public void CalcularCuota_ConDatosInvalidos_Falla(decimal monto, int plazo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraPrestamo.CalcularCuota(monto, 0.025m, plazo));
    }
}
