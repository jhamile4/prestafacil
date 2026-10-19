using PrestaFacil.Application.Prestamos;
using PrestaFacil.Application.Productos;
using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Entities;
using PrestaFacil.UnitTests.Clientes;

namespace PrestaFacil.UnitTests.Prestamos;

public class PrestamoServiceTests
{
    private const int AdminId = 1;
    private const int AsesorId = 2;

    private readonly FakeClienteRepository _clientes = new();
    private readonly FakePrestamoRepository _prestamos = new();
    private readonly FakeProductoRepository _productos = new();
    private readonly FakeUsuarioCambiable _usuario = new(AsesorId);
    private readonly PrestamoService _servicio;
    private readonly Cliente _valeria;

    public PrestamoServiceTests()
    {
        _valeria = new Cliente("72563148", "Valeria", "Chávez Medina", new DateOnly(2000, 5, 18),
            null, null, null, 5200m, 2000m, AsesorId, new DateOnly(2026, 10, 19));
        _clientes.Agregar(_valeria);
        _productos.Agregar(new ProductoPrestamo("Préstamo Personal", 0.35m, 1000m, 15000m, 6, 36, 0.001m));

        _servicio = new PrestamoService(
            _prestamos, _clientes, _productos,
            new SimularPrestamoValidator(), new SolicitarPrestamoValidator(),
            new AprobarPrestamoValidator(), new RechazarPrestamoValidator(),
            _usuario,
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Simular_SinCliente_DevuelveCronogramaSinEvaluacion()
    {
        var simulacion = await _servicio.SimularAsync(new SimularPrestamoRequest(1, 5000m, 12));

        Assert.Equal(488.39m, simulacion.CuotaMensual);
        Assert.Equal(12, simulacion.Cronograma.Count);
        Assert.Equal(simulacion.Monto + simulacion.TotalIntereses, simulacion.TotalAPagar);
        Assert.Null(simulacion.Evaluacion);
    }

    [Fact]
    public async Task Simular_ConClienteMoroso_IncluyeEvaluacionDesfavorable()
    {
        _prestamos.Deuda = new DeudaActualCliente(1, 300m, TieneCuotasVencidas: true);

        var simulacion = await _servicio.SimularAsync(new SimularPrestamoRequest(1, 5000m, 12, ClienteId: _valeria.Id));

        Assert.NotNull(simulacion.Evaluacion);
        Assert.False(simulacion.Evaluacion.Apto);
        Assert.Contains(simulacion.Evaluacion.Reglas, r => r.Codigo == "RN-06" && !r.Cumple);
    }

    [Fact]
    public async Task Solicitar_RegistraAlAsesorYDevuelveLaEvaluacion()
    {
        var detalle = await _servicio.SolicitarAsync(new SolicitarPrestamoRequest(_valeria.Id, 1, 8000m, 24));

        Assert.Equal(EstadoPrestamo.Pendiente, detalle.Prestamo.Estado);
        Assert.Equal(AsesorId, detalle.AsesorId);
        Assert.NotNull(detalle.Evaluacion);
        Assert.True(detalle.Evaluacion.Apto);
    }

    [Fact]
    public async Task Aprobar_VuelveAEvaluarConLaDeudaActual()
    {
        var detalle = await _servicio.SolicitarAsync(new SolicitarPrestamoRequest(_valeria.Id, 1, 8000m, 24));

        // Entre la solicitud y la aprobación, el cliente cayó en mora
        _prestamos.Deuda = new DeudaActualCliente(1, 300m, TieneCuotasVencidas: true);
        _usuario.UserId = AdminId;

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _servicio.AprobarAsync(detalle.Prestamo.Id, new AprobarPrestamoRequest()));
        Assert.Contains("RN-06", ex.Message);
    }

    [Fact]
    public async Task FlujoCompleto_SolicitarAprobarDesembolsar()
    {
        var detalle = await _servicio.SolicitarAsync(new SolicitarPrestamoRequest(_valeria.Id, 1, 8000m, 24));
        var id = detalle.Prestamo.Id;

        _usuario.UserId = AdminId;
        await _servicio.AprobarAsync(id, new AprobarPrestamoRequest("OK"));

        _usuario.UserId = 4;
        var cronograma = await _servicio.DesembolsarAsync(id, new DesembolsarPrestamoRequest());

        Assert.Equal(EstadoPrestamo.Desembolsado, cronograma.Estado);
        Assert.Equal(24, cronograma.Cuotas.Count);
        Assert.Equal(new DateOnly(2026, 11, 19), cronograma.Cuotas[0].FechaVencimiento);
        Assert.Equal(8000m + cronograma.TotalIntereses, cronograma.DeudaPendiente);

        var final = await _servicio.ObtenerAsync(id);
        Assert.Equal([AsesorId, AdminId, 4], final.Historial.Select(h => h.UsuarioId));
    }

    [Fact]
    public async Task Cronograma_DePrestamoPendiente_Falla()
    {
        var detalle = await _servicio.SolicitarAsync(new SolicitarPrestamoRequest(_valeria.Id, 1, 8000m, 24));

        await Assert.ThrowsAsync<DomainException>(() => _servicio.ObtenerCronogramaAsync(detalle.Prestamo.Id));
    }
}

internal class FakePrestamoRepository : IPrestamoRepository
{
    private int _siguienteId = 1;
    private readonly List<Prestamo> _prestamos = [];

    /// <summary>Deuda que devolverá ObtenerDeudaActualAsync; por defecto, sin deudas.</summary>
    public DeudaActualCliente Deuda { get; set; } = new(0, 0m, false);

    public Task<Prestamo?> ObtenerAsync(int id, bool conDetalle, CancellationToken ct = default) =>
        Task.FromResult(_prestamos.FirstOrDefault(p => p.Id == id));

    public Task<(IReadOnlyList<Prestamo> Items, int Total)> BuscarAsync(
        EstadoPrestamo? estado, int? clienteId, int pagina, int tamanoPagina, CancellationToken ct = default) =>
        Task.FromResult<(IReadOnlyList<Prestamo>, int)>((_prestamos, _prestamos.Count));

    public Task<DeudaActualCliente> ObtenerDeudaActualAsync(
        int clienteId, int? excluirPrestamoId, DateOnly hoy, CancellationToken ct = default) =>
        Task.FromResult(Deuda);

    public void Agregar(Prestamo prestamo)
    {
        // Simula el IDENTITY y la columna calculada Codigo de SQL Server
        var id = _siguienteId++;
        typeof(Prestamo).GetProperty(nameof(Prestamo.Id))!.SetValue(prestamo, id);
        typeof(Prestamo).GetProperty(nameof(Prestamo.Codigo))!.SetValue(prestamo, $"PR-{id:000000}");
        _prestamos.Add(prestamo);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal class FakeProductoRepository : IProductoRepository
{
    private readonly List<ProductoPrestamo> _productos = [];

    public void Agregar(ProductoPrestamo producto)
    {
        typeof(ProductoPrestamo).GetProperty(nameof(ProductoPrestamo.Id))!.SetValue(producto, _productos.Count + 1);
        _productos.Add(producto);
    }

    public Task<IReadOnlyList<ProductoPrestamo>> ListarActivosAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProductoPrestamo>>(_productos);

    public Task<ProductoPrestamo?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_productos.FirstOrDefault(p => p.Id == id));
}

/// <summary>Permite cambiar de usuario a mitad de la prueba (asesor → admin → cajero).</summary>
internal class FakeUsuarioCambiable(int userId) : PrestaFacil.Application.Common.ICurrentUser
{
    public int UserId { get; set; } = userId;
}
