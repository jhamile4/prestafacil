using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Entities;
using PrestaFacil.Domain.Services;

namespace PrestaFacil.UnitTests.Prestamos;

public class PagoTests
{
    private const int AsesorId = 2, AdminId = 1, CajeroId = 4;

    private static readonly ProductoPrestamo Personal =
        new("Préstamo Personal", 0.35m, 1000m, 15000m, 6, 36, 0.001m);
    private static readonly ProductoPrestamo Emprendedor =
        new("Préstamo Emprendedor", 0.28m, 3000m, 30000m, 6, 24, 0.0008m);

    /// <summary>Préstamo desembolsado, con Ids asignados como si viniera de la BD.</summary>
    private static Prestamo PrestamoDesembolsado(
        ProductoPrestamo producto, decimal monto, int plazo, DateOnly fechaDesembolso)
    {
        var ahora = fechaDesembolso.ToDateTime(new TimeOnly(9, 0));
        var cliente = new Cliente("41236578", "Jorge Luis", "Ramírez Castillo", new DateOnly(1985, 7, 22),
            null, null, null, 50000m, 1000m, AsesorId, fechaDesembolso);

        var prestamo = Prestamo.Solicitar(cliente, producto, monto, plazo, AsesorId, ahora);
        var evaluacion = EvaluadorCredito.Evaluar(new SituacionCliente(true, 49000m, 0, 0m, false), prestamo.CuotaMensual);
        prestamo.Aprobar(AdminId, evaluacion, ahora);
        prestamo.Desembolsar(CajeroId, fechaDesembolso, ahora);

        AsignarId(prestamo, 1);
        typeof(Prestamo).GetProperty(nameof(Prestamo.Codigo))!.SetValue(prestamo, "PR-000001");
        return prestamo;
    }

    private static Pago Pagar(Prestamo prestamo, decimal monto, DateOnly fecha, int id = 0, int hora = 10)
    {
        var pago = prestamo.RegistrarPago(monto, MedioPago.Efectivo, null, CajeroId, fecha.ToDateTime(new TimeOnly(hora, 0)));
        if (id > 0) AsignarId(pago, id);
        return pago;
    }

    private static void AsignarId(object entidad, int id) =>
        entidad.GetType().GetProperty("Id")!.SetValue(entidad, id);

    // ---------- Aplicación de pagos (RN-09) ----------

    [Fact]
    public void PagoPuntualExacto_CancelaLaCuotaSinMora()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));

        var pago = Pagar(prestamo, 488.39m, new DateOnly(2026, 2, 10));

        var cuota1 = prestamo.Cuotas[0];
        Assert.Equal(EstadoCuota.Pagada, cuota1.Estado);
        Assert.Equal(new DateOnly(2026, 2, 10), cuota1.FechaPagoTotal);
        var detalle = Assert.Single(pago.Detalles);
        Assert.Equal((0m, 126.62m, 361.77m), (detalle.MontoMora, detalle.MontoInteres, detalle.MontoCapital));
    }

    [Fact]
    public void PagoConAtraso_CobraPrimeroLaMoraYLaCuotaSigueVencida()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));

        // 10 días tarde: mora = 488.39 × 0.001 × 10 = 4.88
        var pago = Pagar(prestamo, 488.39m, new DateOnly(2026, 2, 20));

        var detalle = Assert.Single(pago.Detalles);
        Assert.Equal(4.88m, detalle.MontoMora);
        Assert.Equal(126.62m, detalle.MontoInteres);
        Assert.Equal(356.89m, detalle.MontoCapital);
        Assert.Equal(EstadoCuota.Vencida, prestamo.Cuotas[0].Estado);
        Assert.Equal(4.88m, prestamo.Cuotas[0].Pendiente);
    }

    [Fact]
    public void PagoParcialAntesDelVencimiento_CubrePrimeroElInteres()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));

        var pago = Pagar(prestamo, 200m, new DateOnly(2026, 2, 1));

        var detalle = Assert.Single(pago.Detalles);
        Assert.Equal((0m, 126.62m, 73.38m), (detalle.MontoMora, detalle.MontoInteres, detalle.MontoCapital));
        Assert.Equal(EstadoCuota.Parcial, prestamo.Cuotas[0].Estado);
    }

    [Fact]
    public void PagoQueCubreVariasCuotas_EmpiezaPorLaMasAntigua()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));

        var pago = Pagar(prestamo, 1000m, new DateOnly(2026, 2, 5));

        Assert.Equal([1, 2, 3], pago.Detalles.Select(d => d.Cuota.NumeroCuota));
        Assert.Equal(1000m, pago.Detalles.Sum(d => d.Total));
        Assert.Equal(EstadoCuota.Pagada, prestamo.Cuotas[0].Estado);
        Assert.Equal(EstadoCuota.Pagada, prestamo.Cuotas[1].Estado);
        Assert.Equal(EstadoCuota.Parcial, prestamo.Cuotas[2].Estado);
    }

    [Fact]
    public void PagoMayorALaDeuda_FallaPorRN10()
    {
        var prestamo = PrestamoDesembolsado(Personal, 1000m, 6, new DateOnly(2026, 1, 10));

        var ex = Assert.Throws<DomainException>(() => Pagar(prestamo, 5000m, new DateOnly(2026, 2, 1)));

        Assert.Contains("RN-10", ex.Message);
        Assert.Empty(prestamo.Pagos);
    }

    [Fact]
    public void PagarTodaLaDeuda_DejaElPrestamoPagado()
    {
        var prestamo = PrestamoDesembolsado(Personal, 1000m, 6, new DateOnly(2026, 1, 10));
        var deuda = prestamo.DeudaPendiente;

        Pagar(prestamo, deuda, new DateOnly(2026, 2, 1));

        Assert.Equal(EstadoPrestamo.Pagado, prestamo.Estado);
        Assert.All(prestamo.Cuotas, c => Assert.Equal(EstadoCuota.Pagada, c.Estado));
        Assert.Equal(EstadoPrestamo.Pagado, prestamo.Historial[^1].EstadoNuevo);
    }

    [Theory]
    [InlineData(MedioPago.Yape)]
    [InlineData(MedioPago.Plin)]
    [InlineData(MedioPago.Transferencia)]
    public void PagoDigitalSinNumeroDeOperacion_Falla(MedioPago medio)
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));

        Assert.Throws<DomainException>(() =>
            prestamo.RegistrarPago(100m, medio, " ", CajeroId, new DateTime(2026, 2, 1)));
    }

    [Fact]
    public void PagoEnPrestamoNoDesembolsado_Falla()
    {
        var cliente = new Cliente("41236578", "Jorge", "Ramírez", new DateOnly(1985, 7, 22),
            null, null, null, 5000m, 1000m, AsesorId, new DateOnly(2026, 1, 1));
        var prestamo = Prestamo.Solicitar(cliente, Personal, 5000m, 12, AsesorId, new DateTime(2026, 1, 1));

        Assert.Throws<DomainException>(() =>
            prestamo.RegistrarPago(100m, MedioPago.Efectivo, null, CajeroId, new DateTime(2026, 2, 1)));
    }

    // ---------- Mora (RN-08) ----------

    [Fact]
    public void CalcularDeuda_ElCasoDeJorge_CoincideConElProcedimientoDeSqlServer()
    {
        // PR-000002 de los datos de prueba: 15,000 a 18 meses, desembolsado el 19/04/2026, 3 cuotas pagadas
        var prestamo = PrestamoDesembolsado(Emprendedor, 15000m, 18, new DateOnly(2026, 4, 19));
        foreach (var vencimiento in prestamo.Cuotas.Take(3).Select(c => c.FechaVencimiento).ToList())
            Pagar(prestamo, 1007.45m, vencimiento);

        var deuda = prestamo.CalcularDeuda(new DateOnly(2026, 10, 19));

        // sp_ActualizarCuotasVencidas dio 49.16 (61 días) y 24.18 (30 días)
        Assert.Equal(49.16m, prestamo.Cuotas[3].Mora);
        Assert.Equal(24.18m, prestamo.Cuotas[4].Mora);
        Assert.Equal(2, deuda.CuotasVencidas);
        Assert.Equal(61, deuda.DiasAtrasoMaximo);
        Assert.Equal(73.34m, deuda.MoraPendiente);
        Assert.Equal(2014.90m + 73.34m, deuda.TotalParaPonerseAlDia);
        Assert.Equal(6, deuda.ProximaCuota!.NumeroCuota);
    }

    [Fact]
    public void ActualizarMora_EsIdempotente()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));
        var corte = new DateOnly(2026, 3, 1);

        prestamo.ActualizarMora(corte);
        var primera = prestamo.Cuotas.Sum(c => c.Mora);
        prestamo.ActualizarMora(corte);

        Assert.Equal(primera, prestamo.Cuotas.Sum(c => c.Mora));
    }

    [Fact]
    public void CuotaNoVencida_NoGeneraMora()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));

        var deuda = prestamo.CalcularDeuda(new DateOnly(2026, 2, 10)); // vence hoy: aún no hay atraso

        Assert.Equal(0, deuda.CuotasVencidas);
        Assert.Equal(0m, deuda.MoraPendiente);
    }

    [Fact]
    public void Mora_NuncaBajaDeLoYaPagado()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));
        Pagar(prestamo, 300m, new DateOnly(2026, 3, 12)); // paga mora acumulada + parte de la cuota 1
        var moraPagada = prestamo.Cuotas[0].MoraPagada;

        prestamo.ActualizarMora(new DateOnly(2026, 3, 13));

        Assert.True(prestamo.Cuotas[0].Mora >= moraPagada);
    }

    // ---------- Anulación (RN-11) ----------

    [Fact]
    public void AnularUltimoPago_RevierteLosAbonos()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));
        var deudaInicial = prestamo.DeudaPendiente;
        var pago = Pagar(prestamo, 1000m, new DateOnly(2026, 2, 5), id: 7);

        prestamo.AnularPago(7, AdminId, "Billete falso", new DateTime(2026, 2, 5, 11, 0, 0));

        Assert.True(pago.Anulado);
        Assert.Equal("Billete falso", pago.MotivoAnulacion);
        Assert.Equal(deudaInicial, prestamo.DeudaPendiente);
        Assert.All(prestamo.Cuotas, c => Assert.Equal(EstadoCuota.Pendiente, c.Estado));
    }

    [Fact]
    public void AnularPagoQueNoEsElUltimo_Falla()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));
        Pagar(prestamo, 488.39m, new DateOnly(2026, 2, 10), id: 1);
        Pagar(prestamo, 488.39m, new DateOnly(2026, 3, 10), id: 2);

        var ex = Assert.Throws<DomainException>(() =>
            prestamo.AnularPago(1, AdminId, "Error", new DateTime(2026, 3, 10, 12, 0, 0)));
        Assert.Contains("último pago", ex.Message);
    }

    [Fact]
    public void AnularDosVeces_Falla()
    {
        var prestamo = PrestamoDesembolsado(Personal, 5000m, 12, new DateOnly(2026, 1, 10));
        Pagar(prestamo, 488.39m, new DateOnly(2026, 2, 10), id: 1);
        prestamo.AnularPago(1, AdminId, "Error", new DateTime(2026, 2, 10, 12, 0, 0));

        Assert.Throws<DomainException>(() =>
            prestamo.AnularPago(1, AdminId, "Otra vez", new DateTime(2026, 2, 10, 13, 0, 0)));
    }

    [Fact]
    public void AnularElPagoQueCancelabaElPrestamo_LoVuelveADesembolsado()
    {
        var prestamo = PrestamoDesembolsado(Personal, 1000m, 6, new DateOnly(2026, 1, 10));
        Pagar(prestamo, prestamo.DeudaPendiente, new DateOnly(2026, 2, 1), id: 1);
        Assert.Equal(EstadoPrestamo.Pagado, prestamo.Estado);

        prestamo.AnularPago(1, AdminId, "Transferencia rebotada", new DateTime(2026, 2, 1, 12, 0, 0));

        Assert.Equal(EstadoPrestamo.Desembolsado, prestamo.Estado);
        Assert.Equal(EstadoPrestamo.Desembolsado, prestamo.Historial[^1].EstadoNuevo);
    }
}
