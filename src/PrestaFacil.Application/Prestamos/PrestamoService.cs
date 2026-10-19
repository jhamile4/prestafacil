using FluentValidation;
using PrestaFacil.Application.Clientes;
using PrestaFacil.Application.Common;
using PrestaFacil.Application.Productos;
using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Entities;
using PrestaFacil.Domain.Services;

namespace PrestaFacil.Application.Prestamos;

public interface IPrestamoService
{
    Task<SimulacionResponse> SimularAsync(SimularPrestamoRequest request, CancellationToken ct = default);
    Task<PrestamoDetalleResponse> SolicitarAsync(SolicitarPrestamoRequest request, CancellationToken ct = default);
    Task<PagedResult<PrestamoResponse>> BuscarAsync(BuscarPrestamosQuery query, CancellationToken ct = default);
    Task<PrestamoDetalleResponse> ObtenerAsync(int id, CancellationToken ct = default);
    Task<EvaluacionResponse> EvaluarAsync(int id, CancellationToken ct = default);
    Task<PrestamoDetalleResponse> AprobarAsync(int id, AprobarPrestamoRequest request, CancellationToken ct = default);
    Task<PrestamoDetalleResponse> RechazarAsync(int id, RechazarPrestamoRequest request, CancellationToken ct = default);
    Task<CronogramaResponse> DesembolsarAsync(int id, DesembolsarPrestamoRequest request, CancellationToken ct = default);
    Task<CronogramaResponse> ObtenerCronogramaAsync(int id, CancellationToken ct = default);
}

public class PrestamoService(
    IPrestamoRepository prestamos,
    IClienteRepository clientes,
    IProductoRepository productos,
    IValidator<SimularPrestamoRequest> validadorSimular,
    IValidator<SolicitarPrestamoRequest> validadorSolicitar,
    IValidator<AprobarPrestamoRequest> validadorAprobar,
    IValidator<RechazarPrestamoRequest> validadorRechazar,
    ICurrentUser usuarioActual,
    TimeProvider reloj) : IPrestamoService
{
    private DateTime Ahora => reloj.GetLocalNow().DateTime;
    private DateOnly Hoy => DateOnly.FromDateTime(Ahora);

    /// <summary>RF-07: cuota y cronograma sin guardar nada.</summary>
    public async Task<SimulacionResponse> SimularAsync(SimularPrestamoRequest request, CancellationToken ct = default)
    {
        await validadorSimular.ValidateAndThrowAsync(request, ct);

        var producto = await ObtenerProductoAsync(request.ProductoId, ct);
        Prestamo.ValidarLimites(producto, request.Monto, request.PlazoMeses);

        var tem = CalculadoraPrestamo.CalcularTem(producto.Tea);
        var cuota = CalculadoraPrestamo.CalcularCuota(request.Monto, tem, request.PlazoMeses);
        var cronograma = CalculadoraPrestamo.GenerarCronograma(
            request.Monto, tem, request.PlazoMeses, cuota, request.FechaDesembolso ?? Hoy);

        EvaluacionResponse? evaluacion = null;
        if (request.ClienteId is { } clienteId)
        {
            var cliente = await ObtenerClienteAsync(clienteId, ct);
            evaluacion = EvaluacionResponse.Desde(await EvaluarClienteAsync(cliente, cuota, null, ct));
        }

        var totalIntereses = cronograma.Sum(c => c.Interes);
        return new SimulacionResponse(
            producto.Nombre, request.Monto, request.PlazoMeses, producto.Tea, tem, cuota,
            totalIntereses, request.Monto + totalIntereses,
            cronograma.Select(c => new CuotaSimuladaResponse(
                c.Numero, c.FechaVencimiento, c.SaldoInicial, c.Capital, c.Interes, c.MontoCuota, c.SaldoFinal)).ToList(),
            evaluacion);
    }

    /// <summary>RF-06 y RF-08: registra la solicitud y devuelve su evaluación.</summary>
    public async Task<PrestamoDetalleResponse> SolicitarAsync(SolicitarPrestamoRequest request, CancellationToken ct = default)
    {
        await validadorSolicitar.ValidateAndThrowAsync(request, ct);

        var cliente = await ObtenerClienteAsync(request.ClienteId, ct);
        var producto = await ObtenerProductoAsync(request.ProductoId, ct);

        var prestamo = Prestamo.Solicitar(cliente, producto, request.Monto, request.PlazoMeses, usuarioActual.UserId, Ahora);
        prestamos.Agregar(prestamo);
        await prestamos.GuardarCambiosAsync(ct);

        return await ObtenerAsync(prestamo.Id, ct);
    }

    public async Task<PagedResult<PrestamoResponse>> BuscarAsync(BuscarPrestamosQuery query, CancellationToken ct = default)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamano = Math.Clamp(query.TamanoPagina, 1, 100);

        var (items, total) = await prestamos.BuscarAsync(query.Estado, query.ClienteId, pagina, tamano, ct);
        return new PagedResult<PrestamoResponse>(items.Select(PrestamoResponse.Desde).ToList(), total, pagina, tamano);
    }

    public async Task<PrestamoDetalleResponse> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await ObtenerPrestamoAsync(id, conDetalle: true, ct);

        EvaluacionResponse? evaluacion = null;
        if (prestamo.Estado == EstadoPrestamo.Pendiente)
            evaluacion = EvaluacionResponse.Desde(await EvaluarPrestamoAsync(prestamo, ct));

        return ADetalle(prestamo, evaluacion);
    }

    /// <summary>RF-08: evaluación a la fecha (la situación del cliente puede cambiar).</summary>
    public async Task<EvaluacionResponse> EvaluarAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await ObtenerPrestamoAsync(id, conDetalle: false, ct);
        return EvaluacionResponse.Desde(await EvaluarPrestamoAsync(prestamo, ct));
    }

    /// <summary>RF-09: se vuelve a evaluar justo antes de aprobar.</summary>
    public async Task<PrestamoDetalleResponse> AprobarAsync(int id, AprobarPrestamoRequest request, CancellationToken ct = default)
    {
        await validadorAprobar.ValidateAndThrowAsync(request, ct);

        var prestamo = await ObtenerPrestamoAsync(id, conDetalle: true, ct);
        var evaluacion = await EvaluarPrestamoAsync(prestamo, ct);

        prestamo.Aprobar(usuarioActual.UserId, evaluacion, Ahora, request.Comentario);
        await prestamos.GuardarCambiosAsync(ct);

        return ADetalle(prestamo, null);
    }

    public async Task<PrestamoDetalleResponse> RechazarAsync(int id, RechazarPrestamoRequest request, CancellationToken ct = default)
    {
        await validadorRechazar.ValidateAndThrowAsync(request, ct);

        var prestamo = await ObtenerPrestamoAsync(id, conDetalle: true, ct);
        prestamo.Rechazar(usuarioActual.UserId, request.Motivo, Ahora);
        await prestamos.GuardarCambiosAsync(ct);

        return ADetalle(prestamo, null);
    }

    /// <summary>RF-10: desembolsa y guarda el cronograma en una sola transacción.</summary>
    public async Task<CronogramaResponse> DesembolsarAsync(int id, DesembolsarPrestamoRequest request, CancellationToken ct = default)
    {
        var prestamo = await ObtenerPrestamoAsync(id, conDetalle: true, ct);
        prestamo.Desembolsar(usuarioActual.UserId, request.FechaDesembolso ?? Hoy, Ahora);
        await prestamos.GuardarCambiosAsync(ct);

        return ACronograma(prestamo);
    }

    /// <summary>RF-11</summary>
    public async Task<CronogramaResponse> ObtenerCronogramaAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await ObtenerPrestamoAsync(id, conDetalle: true, ct);
        if (prestamo.Estado is not (EstadoPrestamo.Desembolsado or EstadoPrestamo.Pagado))
            throw new DomainException(
                $"El préstamo {prestamo.Codigo} aún no tiene cronograma porque no se ha desembolsado. Use el simulador.");

        return ACronograma(prestamo);
    }

    // ---------- Apoyo ----------

    private Task<ResultadoEvaluacion> EvaluarPrestamoAsync(Prestamo prestamo, CancellationToken ct) =>
        EvaluarClienteAsync(prestamo.Cliente, prestamo.CuotaMensual, prestamo.Id, ct);

    private async Task<ResultadoEvaluacion> EvaluarClienteAsync(
        Cliente cliente, decimal cuotaNueva, int? excluirPrestamoId, CancellationToken ct)
    {
        var deuda = await prestamos.ObtenerDeudaActualAsync(cliente.Id, excluirPrestamoId, Hoy, ct);
        var situacion = new SituacionCliente(
            cliente.Activo, cliente.IngresoNeto, deuda.PrestamosActivos, deuda.CuotasMensuales, deuda.TieneCuotasVencidas);
        return EvaluadorCredito.Evaluar(situacion, cuotaNueva);
    }

    private async Task<Prestamo> ObtenerPrestamoAsync(int id, bool conDetalle, CancellationToken ct) =>
        await prestamos.ObtenerAsync(id, conDetalle, ct)
            ?? throw new NotFoundException($"No existe el préstamo con id {id}.");

    private async Task<Cliente> ObtenerClienteAsync(int id, CancellationToken ct) =>
        await clientes.ObtenerPorIdAsync(id, ct)
            ?? throw new NotFoundException($"No existe el cliente con id {id}.");

    private async Task<ProductoPrestamo> ObtenerProductoAsync(int id, CancellationToken ct) =>
        await productos.ObtenerPorIdAsync(id, ct)
            ?? throw new NotFoundException($"No existe el producto con id {id}.");

    private static PrestamoDetalleResponse ADetalle(Prestamo p, EvaluacionResponse? evaluacion) => new(
        PrestamoResponse.Desde(p), p.Tem, p.TasaMoraDiaria, p.AsesorId, p.AprobadoPorId, p.DesembolsadoPorId,
        p.Historial.OrderBy(h => h.Fecha).ThenBy(h => h.Id)
            .Select(h => new HistorialResponse(h.EstadoAnterior, h.EstadoNuevo, h.UsuarioId, h.Fecha, h.Comentario))
            .ToList(),
        evaluacion);

    private static CronogramaResponse ACronograma(Prestamo p)
    {
        var cuotas = p.Cuotas.OrderBy(c => c.NumeroCuota).ToList();
        return new CronogramaResponse(
            p.Codigo, p.Estado, p.Monto, p.CuotaMensual,
            cuotas.Sum(c => c.Interes),
            cuotas.Sum(c => c.TotalPagado),
            cuotas.Sum(c => c.Pendiente),
            cuotas.Select(CuotaResponse.Desde).ToList());
    }
}
