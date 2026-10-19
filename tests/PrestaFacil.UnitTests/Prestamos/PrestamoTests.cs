using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Entities;
using PrestaFacil.Domain.Services;

namespace PrestaFacil.UnitTests.Prestamos;

public class PrestamoTests
{
    private const int AsesorId = 2;
    private const int AdminId = 1;
    private const int CajeroId = 4;
    private static readonly DateTime Ahora = new(2026, 10, 19, 10, 0, 0);

    private static readonly ProductoPrestamo Personal =
        new("Préstamo Personal", 0.35m, 1000m, 15000m, 6, 36, 0.001m);

    private static Cliente NuevoCliente() =>
        new("72563148", "Valeria", "Chávez Medina", new DateOnly(2000, 5, 18),
            null, null, null, 5200m, 2000m, AsesorId, DateOnly.FromDateTime(Ahora));

    private static ResultadoEvaluacion EvaluacionFavorable() =>
        EvaluadorCredito.Evaluar(new SituacionCliente(true, 3200m, 0, 0m, false), 448.91m);

    private static Prestamo NuevaSolicitud(decimal monto = 8000m, int plazo = 24) =>
        Prestamo.Solicitar(NuevoCliente(), Personal, monto, plazo, AsesorId, Ahora);

    [Fact]
    public void Solicitar_CopiaLasTasasDelProductoYCalculaLaCuota()
    {
        var prestamo = NuevaSolicitud();

        Assert.Equal(EstadoPrestamo.Pendiente, prestamo.Estado);
        Assert.Equal(0.35m, prestamo.Tea);
        Assert.Equal(0.001m, prestamo.TasaMoraDiaria);
        Assert.Equal(448.91m, prestamo.CuotaMensual);
        var registro = Assert.Single(prestamo.Historial);
        Assert.Null(registro.EstadoAnterior);
        Assert.Equal(EstadoPrestamo.Pendiente, registro.EstadoNuevo);
    }

    [Theory]
    [InlineData(999, 12)]     // monto menor al mínimo
    [InlineData(15001, 12)]   // monto mayor al máximo
    [InlineData(5000, 5)]     // plazo menor al mínimo
    [InlineData(5000, 37)]    // plazo mayor al máximo
    [InlineData(5000.555, 12)] // más de 2 decimales
    public void Solicitar_FueraDeLosLimitesDelProducto_Falla(decimal monto, int plazo)
    {
        Assert.Throws<DomainException>(() => NuevaSolicitud(monto, plazo));
    }

    [Fact]
    public void Solicitar_ClienteDesactivado_Falla()
    {
        var cliente = NuevoCliente();
        cliente.Desactivar();

        Assert.Throws<DomainException>(() =>
            Prestamo.Solicitar(cliente, Personal, 5000m, 12, AsesorId, Ahora));
    }

    [Fact]
    public void Aprobar_ConEvaluacionFavorable_CambiaEstadoYRegistraHistorial()
    {
        var prestamo = NuevaSolicitud();

        prestamo.Aprobar(AdminId, EvaluacionFavorable(), Ahora);

        Assert.Equal(EstadoPrestamo.Aprobado, prestamo.Estado);
        Assert.Equal(AdminId, prestamo.AprobadoPorId);
        Assert.Equal(2, prestamo.Historial.Count);
        Assert.Equal(EstadoPrestamo.Pendiente, prestamo.Historial[^1].EstadoAnterior);
    }

    [Fact]
    public void Aprobar_ElMismoAsesor_FallaPorSegregacionDeFunciones()
    {
        var prestamo = NuevaSolicitud();

        var ex = Assert.Throws<DomainException>(() => prestamo.Aprobar(AsesorId, EvaluacionFavorable(), Ahora));
        Assert.Contains("segregación", ex.Message);
    }

    [Fact]
    public void Aprobar_ConEvaluacionDesfavorable_FallaYExplicaElMotivo()
    {
        var prestamo = NuevaSolicitud();
        var desfavorable = EvaluadorCredito.Evaluar(new SituacionCliente(true, 1000m, 0, 0m, false), 976.79m);

        var ex = Assert.Throws<DomainException>(() => prestamo.Aprobar(AdminId, desfavorable, Ahora));

        Assert.Contains("RN-04", ex.Message);
        Assert.Equal(EstadoPrestamo.Pendiente, prestamo.Estado);
    }

    [Fact]
    public void Rechazar_SinMotivo_Falla()
    {
        var prestamo = NuevaSolicitud();

        Assert.Throws<DomainException>(() => prestamo.Rechazar(AdminId, "  ", Ahora));
    }

    [Fact]
    public void Rechazar_GuardaElMotivo()
    {
        var prestamo = NuevaSolicitud();

        prestamo.Rechazar(AdminId, "Ingresos no sustentados", Ahora);

        Assert.Equal(EstadoPrestamo.Rechazado, prestamo.Estado);
        Assert.Equal("Ingresos no sustentados", prestamo.MotivoRechazo);
    }

    [Fact]
    public void Desembolsar_GeneraElCronogramaCompleto()
    {
        var prestamo = NuevaSolicitud();
        prestamo.Aprobar(AdminId, EvaluacionFavorable(), Ahora);

        prestamo.Desembolsar(CajeroId, new DateOnly(2026, 10, 19), Ahora);

        Assert.Equal(EstadoPrestamo.Desembolsado, prestamo.Estado);
        Assert.Equal(24, prestamo.Cuotas.Count);
        Assert.Equal(8000m, prestamo.Cuotas.Sum(c => c.Capital));
        Assert.Equal(new DateOnly(2026, 11, 19), prestamo.Cuotas[0].FechaVencimiento);
        Assert.All(prestamo.Cuotas, c => Assert.Equal(EstadoCuota.Pendiente, c.Estado));
        Assert.Equal(3, prestamo.Historial.Count);
    }

    [Fact]
    public void Desembolsar_SinAprobar_Falla()
    {
        var prestamo = NuevaSolicitud();

        Assert.Throws<DomainException>(() => prestamo.Desembolsar(CajeroId, new DateOnly(2026, 10, 19), Ahora));
        Assert.Empty(prestamo.Cuotas);
    }

    [Fact]
    public void Desembolsar_ConFechaFutura_Falla()
    {
        var prestamo = NuevaSolicitud();
        prestamo.Aprobar(AdminId, EvaluacionFavorable(), Ahora);

        Assert.Throws<DomainException>(() => prestamo.Desembolsar(CajeroId, new DateOnly(2026, 10, 20), Ahora));
    }

    [Fact]
    public void Desembolsar_DosVeces_Falla()
    {
        var prestamo = NuevaSolicitud();
        prestamo.Aprobar(AdminId, EvaluacionFavorable(), Ahora);
        prestamo.Desembolsar(CajeroId, new DateOnly(2026, 10, 19), Ahora);

        Assert.Throws<DomainException>(() => prestamo.Desembolsar(CajeroId, new DateOnly(2026, 10, 19), Ahora));
        Assert.Equal(24, prestamo.Cuotas.Count);
    }
}
