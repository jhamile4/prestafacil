using PrestaFacil.Domain.Services;

namespace PrestaFacil.UnitTests.Prestamos;

public class EvaluadorCreditoTests
{
    private static SituacionCliente ClienteSano(decimal ingresoNeto = 3200m) =>
        new(ClienteActivo: true, IngresoNeto: ingresoNeto, PrestamosActivos: 0,
            CuotasMensualesActuales: 0m, TieneCuotasVencidas: false);

    [Fact]
    public void Evaluar_ClienteSinDeudas_EsApto()
    {
        // Valeria (PR-000005): neto S/ 3,200, cuota S/ 448.91
        var resultado = EvaluadorCredito.Evaluar(ClienteSano(3200m), 448.91m);

        Assert.True(resultado.Apto);
        Assert.Equal(1280.00m, resultado.LimiteCuotas);
        Assert.Equal(4, resultado.Reglas.Count);
    }

    [Fact]
    public void Evaluar_CuotaMayorAl40PorCiento_NoEsApto()
    {
        // Silvia (PR-000007): neto S/ 1,000 → límite S/ 400, cuota S/ 976.79
        var resultado = EvaluadorCredito.Evaluar(ClienteSano(1000m), 976.79m);

        Assert.False(resultado.Apto);
        Assert.Equal("RN-04", Assert.Single(resultado.Incumplidas).Codigo);
    }

    [Fact]
    public void Evaluar_CuotaExactamenteEnElLimite_EsApto()
    {
        var resultado = EvaluadorCredito.Evaluar(ClienteSano(1000m), 400m);

        Assert.True(resultado.Apto);
    }

    [Fact]
    public void Evaluar_SumaLasCuotasDePrestamosActuales()
    {
        var cliente = ClienteSano(2000m) with { PrestamosActivos = 1, CuotasMensualesActuales = 600m };

        // 600 + 250 = 850 > 800 (40 % de 2000)
        var resultado = EvaluadorCredito.Evaluar(cliente, 250m);

        Assert.False(resultado.Apto);
        Assert.Equal("RN-04", Assert.Single(resultado.Incumplidas).Codigo);
    }

    [Fact]
    public void Evaluar_ConDosPrestamosActivos_NoEsApto()
    {
        var cliente = ClienteSano(10000m) with { PrestamosActivos = 2 };

        var resultado = EvaluadorCredito.Evaluar(cliente, 100m);

        Assert.Equal("RN-05", Assert.Single(resultado.Incumplidas).Codigo);
    }

    [Fact]
    public void Evaluar_ConCuotasVencidas_NoEsApto()
    {
        var cliente = ClienteSano() with { TieneCuotasVencidas = true };

        var resultado = EvaluadorCredito.Evaluar(cliente, 100m);

        Assert.Equal("RN-06", Assert.Single(resultado.Incumplidas).Codigo);
    }

    [Fact]
    public void Evaluar_ConVariosProblemas_LosReportaTodos()
    {
        var cliente = new SituacionCliente(false, 1000m, 2, 300m, true);

        var resultado = EvaluadorCredito.Evaluar(cliente, 500m);

        Assert.Equal(["RN-07", "RN-04", "RN-05", "RN-06"], resultado.Incumplidas.Select(r => r.Codigo));
    }

    [Fact]
    public void Evaluar_ConIngresoNetoNegativo_ElLimiteEsCero()
    {
        var resultado = EvaluadorCredito.Evaluar(ClienteSano(-500m), 10m);

        Assert.Equal(0m, resultado.LimiteCuotas);
        Assert.False(resultado.Apto);
    }
}
