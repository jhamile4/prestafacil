using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Services;

namespace PrestaFacil.Domain.Entities;

public class Prestamo
{
    private readonly List<Cuota> _cuotas = [];
    private readonly List<HistorialEstadoPrestamo> _historial = [];

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
