using FluentValidation;
using PrestaFacil.Application.Common;
using PrestaFacil.Application.Prestamos;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Pagos;

public interface IPagoService
{
    Task<DeudaResponse> ConsultarDeudaAsync(int prestamoId, CancellationToken ct = default);
    Task<PagoRegistradoResponse> RegistrarAsync(int prestamoId, RegistrarPagoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PagoResponse>> ListarAsync(int prestamoId, CancellationToken ct = default);
    Task<PagoResponse> AnularAsync(int prestamoId, int pagoId, AnularPagoRequest request, CancellationToken ct = default);
}

public class PagoService(
    IPrestamoRepository prestamos,
    IValidator<RegistrarPagoRequest> validadorRegistrar,
    IValidator<AnularPagoRequest> validadorAnular,
    ICurrentUser usuarioActual,
    TimeProvider reloj) : IPagoService
{
    private DateTime Ahora => reloj.GetLocalNow().DateTime;
    private DateOnly Hoy => DateOnly.FromDateTime(Ahora);

    /// <summary>Cuánto debe hoy, con la mora calculada a la fecha. No guarda nada.</summary>
    public async Task<DeudaResponse> ConsultarDeudaAsync(int prestamoId, CancellationToken ct = default)
    {
        var prestamo = await ObtenerPrestamoAsync(prestamoId, ct);
        return DeudaResponse.Desde(prestamo, prestamo.CalcularDeuda(Hoy));
    }

    /// <summary>
    /// RF-12 y RF-13. Pago, abonos a cuotas, mora actualizada y cambio de estado
    /// se guardan en un solo SaveChanges, que EF Core ejecuta en una transacción (RNF-02).
    /// </summary>
    public async Task<PagoRegistradoResponse> RegistrarAsync(
        int prestamoId, RegistrarPagoRequest request, CancellationToken ct = default)
    {
        await validadorRegistrar.ValidateAndThrowAsync(request, ct);

        var prestamo = await ObtenerPrestamoAsync(prestamoId, ct);
        var pago = prestamo.RegistrarPago(request.Monto, request.MedioPago, request.NumeroOperacion, usuarioActual.UserId, Ahora);
        await prestamos.GuardarCambiosAsync(ct);

        return new PagoRegistradoResponse(
            PagoResponse.Desde(pago, prestamo.Codigo),
            prestamo.Estado,
            DeudaResponse.Desde(prestamo, prestamo.CalcularDeuda(Hoy)));
    }

    public async Task<IReadOnlyList<PagoResponse>> ListarAsync(int prestamoId, CancellationToken ct = default)
    {
        var prestamo = await ObtenerPrestamoAsync(prestamoId, ct);
        return prestamo.Pagos
            .OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.Id)
            .Select(p => PagoResponse.Desde(p, prestamo.Codigo))
            .ToList();
    }

    /// <summary>RN-11: anula el último pago válido y revierte sus abonos.</summary>
    public async Task<PagoResponse> AnularAsync(
        int prestamoId, int pagoId, AnularPagoRequest request, CancellationToken ct = default)
    {
        await validadorAnular.ValidateAndThrowAsync(request, ct);

        var prestamo = await ObtenerPrestamoAsync(prestamoId, ct);
        var pago = prestamo.AnularPago(pagoId, usuarioActual.UserId, request.Motivo, Ahora);
        await prestamos.GuardarCambiosAsync(ct);

        return PagoResponse.Desde(pago, prestamo.Codigo);
    }

    private async Task<Prestamo> ObtenerPrestamoAsync(int id, CancellationToken ct) =>
        await prestamos.ObtenerAsync(id, conDetalle: true, ct)
            ?? throw new NotFoundException($"No existe el préstamo con id {id}.");
}
