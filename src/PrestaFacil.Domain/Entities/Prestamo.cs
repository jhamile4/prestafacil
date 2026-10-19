using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Services;

namespace PrestaFacil.Domain.Entities;

public class Prestamo
{
    private readonly List<Cuota> _cuotas = [];
    private readonly List<HistorialEstadoPrestamo> _historial = [];
    private readonly List<Pago> _pagos = [];

    public int Id { get; private set; }
    /// <summary>PR-000001. Lo calcula SQL Server a partir del Id.</summary>
    public string Codigo { get; private set; } = null!;
    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;
    public int ProductoId { get; private set; }
    public ProductoPrestamo Producto { get; private set; } = null!;
    public decimal Monto { get; private set; }
    public int PlazoMeses { get; private set; }
    public decimal Tea { get; private set; }
    public decimal Tem { get; private set; }
    public decimal CuotaMensual { get; private set; }
    public decimal TasaMoraDiaria { get; private set; }
    public EstadoPrestamo Estado { get; private set; }
    public int AsesorId { get; private set; }
    public DateTime FechaSolicitud { get; private set; }
    public int? AprobadoPorId { get; private set; }
    public DateTime? FechaAprobacion { get; private set; }
    public string? MotivoRechazo { get; private set; }
    public int? DesembolsadoPorId { get; private set; }
    public DateOnly? FechaDesembolso { get; private set; }

    public IReadOnlyList<Cuota> Cuotas => _cuotas;
    public IReadOnlyList<HistorialEstadoPrestamo> Historial => _historial;
    public IReadOnlyList<Pago> Pagos => _pagos;

    /// <summary>Lo que falta pagar, incluida la mora ya calculada.</summary>
    public decimal DeudaPendiente => _cuotas.Sum(c => c.Pendiente);

    private Prestamo() { }

    /// <summary>
    /// Crea la solicitud. Valida RN-02 y copia las tasas del producto (RN-03).
    /// La evaluación crediticia (RN-04 a RN-07) se revisa al aprobar.
    /// </summary>
    public static Prestamo Solicitar(
        Cliente cliente, ProductoPrestamo producto, decimal monto, int plazoMeses, int asesorId, DateTime ahora)
    {
        if (!cliente.Activo)
            throw new DomainException("No se pueden registrar solicitudes de un cliente desactivado (RN-07).");
        if (!producto.Activo)
            throw new DomainException($"El producto {producto.Nombre} no está disponible.");
        ValidarLimites(producto, monto, plazoMeses);

        var tem = CalculadoraPrestamo.CalcularTem(producto.Tea);
        var prestamo = new Prestamo
        {
            ClienteId = cliente.Id,
            Cliente = cliente,
            ProductoId = producto.Id,
            Producto = producto,
            Monto = monto,
            PlazoMeses = plazoMeses,
            Tea = producto.Tea,
            Tem = tem,
            CuotaMensual = CalculadoraPrestamo.CalcularCuota(monto, tem, plazoMeses),
            TasaMoraDiaria = producto.TasaMoraDiaria,
            Estado = EstadoPrestamo.Pendiente,
            AsesorId = asesorId,
            FechaSolicitud = ahora
        };
        prestamo._historial.Add(new(null, EstadoPrestamo.Pendiente, asesorId, ahora, "Solicitud registrada"));
        return prestamo;
    }

    /// <summary>RN-02: monto y plazo dentro de los límites del producto.</summary>
    public static void ValidarLimites(ProductoPrestamo producto, decimal monto, int plazoMeses)
    {
        if (monto < producto.MontoMinimo || monto > producto.MontoMaximo)
            throw new DomainException(
                $"El monto debe estar entre S/ {producto.MontoMinimo:N2} y S/ {producto.MontoMaximo:N2} para {producto.Nombre} (RN-02).");
        if (plazoMeses < producto.PlazoMinimoMeses || plazoMeses > producto.PlazoMaximoMeses)
            throw new DomainException(
                $"El plazo debe estar entre {producto.PlazoMinimoMeses} y {producto.PlazoMaximoMeses} meses para {producto.Nombre} (RN-02).");
        if (decimal.Round(monto, 2) != monto)
            throw new DomainException("El monto admite como máximo 2 decimales.");
    }

    /// <summary>
    /// Aprueba la solicitud si la evaluación es favorable. Quien aprueba no
    /// puede ser el mismo asesor que la registró (segregación de funciones).
    /// </summary>
    public void Aprobar(int adminId, ResultadoEvaluacion evaluacion, DateTime ahora, string? comentario = null)
    {
        ExigirEstado(EstadoPrestamo.Pendiente, "aprobar");
        if (adminId == AsesorId)
            throw new DomainException("Quien registró la solicitud no puede aprobarla (segregación de funciones).");
        if (!evaluacion.Apto)
            throw new DomainException(
                "La solicitud no cumple la evaluación: " +
                string.Join(" ", evaluacion.Incumplidas.Select(r => $"[{r.Codigo}] {r.Detalle}")));

        AprobadoPorId = adminId;
        FechaAprobacion = ahora;
        CambiarEstado(EstadoPrestamo.Aprobado, adminId, ahora, comentario ?? "Cumple las reglas de evaluación");
    }

    public void Rechazar(int adminId, string motivo, DateTime ahora)
    {
        ExigirEstado(EstadoPrestamo.Pendiente, "rechazar");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("Debe indicar el motivo del rechazo.");

        AprobadoPorId = adminId;
        FechaAprobacion = ahora;
        MotivoRechazo = motivo.Trim();
        CambiarEstado(EstadoPrestamo.Rechazado, adminId, ahora, MotivoRechazo);
    }

    /// <summary>Entrega el dinero y genera el cronograma (RF-10).</summary>
    public void Desembolsar(int cajeroId, DateOnly fechaDesembolso, DateTime ahora)
    {
        ExigirEstado(EstadoPrestamo.Aprobado, "desembolsar");
        var hoy = DateOnly.FromDateTime(ahora);
        if (fechaDesembolso > hoy)
            throw new DomainException("La fecha de desembolso no puede ser futura.");
        if (FechaAprobacion is { } aprobacion && fechaDesembolso < DateOnly.FromDateTime(aprobacion))
            throw new DomainException("La fecha de desembolso no puede ser anterior a la aprobación.");

        foreach (var c in CalculadoraPrestamo.GenerarCronograma(Monto, Tem, PlazoMeses, CuotaMensual, fechaDesembolso))
            _cuotas.Add(new Cuota(c.Numero, c.FechaVencimiento, c.SaldoInicial, c.Capital, c.Interes));

        DesembolsadoPorId = cajeroId;
        FechaDesembolso = fechaDesembolso;
        CambiarEstado(EstadoPrestamo.Desembolsado, cajeroId, ahora, $"Desembolso de S/ {Monto:N2}");
    }

    /// <summary>RF-14 / RN-08: calcula la mora de las cuotas vencidas a la fecha de corte.</summary>
    public void ActualizarMora(DateOnly fechaCorte)
    {
        if (Estado != EstadoPrestamo.Desembolsado) return;
        foreach (var cuota in _cuotas)
            cuota.ActualizarMora(fechaCorte, TasaMoraDiaria);
    }

    /// <summary>Situación de la deuda a una fecha. Actualiza la mora en memoria.</summary>
    public ResumenDeuda CalcularDeuda(DateOnly fechaCorte)
    {
        ActualizarMora(fechaCorte);

        var vencidas = _cuotas.Where(c => c.Estado == EstadoCuota.Vencida).ToList();
        var proxima = _cuotas
            .Where(c => c.Pendiente > 0 && c.FechaVencimiento >= fechaCorte)
            .MinBy(c => c.NumeroCuota);

        return new ResumenDeuda(
            fechaCorte,
            vencidas.Count,
            vencidas.Count == 0 ? 0 : vencidas.Max(c => c.DiasAtraso(fechaCorte)),
            vencidas.Sum(c => c.SaldoVencible),
            _cuotas.Sum(c => c.MoraPendiente),
            vencidas.Sum(c => c.Pendiente),
            DeudaPendiente,
            proxima);
    }

    /// <summary>
    /// RF-12 y RF-13: registra el pago y lo aplica desde la cuota más antigua (RN-09).
    /// Antes calcula la mora a la fecha para cobrarla completa.
    /// </summary>
    public Pago RegistrarPago(decimal monto, MedioPago medioPago, string? numeroOperacion, int cajeroId, DateTime ahora)
    {
        ExigirEstado(EstadoPrestamo.Desembolsado, "registrar pagos en");
        var hoy = DateOnly.FromDateTime(ahora);
        ActualizarMora(hoy);

        // RN-10
        var deuda = DeudaPendiente;
        if (monto > deuda)
            throw new DomainException(
                $"El pago de S/ {monto:N2} supera la deuda total del préstamo {Codigo}: S/ {deuda:N2} (RN-10).");

        var pago = new Pago(monto, medioPago, numeroOperacion, cajeroId, ahora);
        var restante = monto;
        foreach (var cuota in _cuotas.Where(c => c.Pendiente > 0).OrderBy(c => c.NumeroCuota))
        {
            if (restante == 0) break;
            var aplicado = cuota.Aplicar(restante, hoy);
            pago.AgregarDetalle(cuota, aplicado.Mora, aplicado.Interes, aplicado.Capital);
            restante -= aplicado.Total;
        }
        _pagos.Add(pago);

        // RF-15
        if (_cuotas.All(c => c.Estado == EstadoCuota.Pagada))
            CambiarEstado(EstadoPrestamo.Pagado, cajeroId, ahora, "Se pagaron todas las cuotas");

        return pago;
    }

    /// <summary>
    /// RN-11: el pago no se borra, se anula y se revierten sus abonos. Solo se
    /// puede anular el último pago válido, porque los posteriores se aplicaron
    /// asumiendo que este existía.
    /// </summary>
    public Pago AnularPago(int pagoId, int adminId, string motivo, DateTime ahora)
    {
        if (Estado is not (EstadoPrestamo.Desembolsado or EstadoPrestamo.Pagado))
            throw new DomainException($"El préstamo {Codigo} no tiene pagos que anular.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("Debe indicar el motivo de la anulación.");

        var pago = _pagos.FirstOrDefault(p => p.Id == pagoId)
            ?? throw new DomainException($"El pago {pagoId} no pertenece al préstamo {Codigo}.");
        if (pago.Anulado)
            throw new DomainException($"El pago {pagoId} ya está anulado.");

        var ultimo = _pagos.Where(p => !p.Anulado).OrderBy(p => p.FechaPago).ThenBy(p => p.Id).Last();
        if (ultimo != pago)
            throw new DomainException(
                $"Solo se puede anular el último pago válido del préstamo (el {ultimo.Id}). Anule primero los posteriores.");

        var hoy = DateOnly.FromDateTime(ahora);
        foreach (var detalle in pago.Detalles)
            detalle.Cuota.Revertir(detalle, hoy);
        pago.Anular(adminId, motivo.Trim(), ahora);

        if (Estado == EstadoPrestamo.Pagado)
            CambiarEstado(EstadoPrestamo.Desembolsado, adminId, ahora, $"Anulación del pago {pago.Id}: {motivo.Trim()}");

        ActualizarMora(hoy);
        return pago;
    }

    private void ExigirEstado(EstadoPrestamo esperado, string accion)
    {
        if (Estado != esperado)
            throw new DomainException(
                $"No se puede {accion} el préstamo {Codigo} porque está {Estado.ToString().ToUpperInvariant()}; debe estar {esperado.ToString().ToUpperInvariant()}.");
    }

    private void CambiarEstado(EstadoPrestamo nuevo, int usuarioId, DateTime fecha, string? comentario)
    {
        _historial.Add(new(Estado, nuevo, usuarioId, fecha, comentario));
        Estado = nuevo;
    }
}
