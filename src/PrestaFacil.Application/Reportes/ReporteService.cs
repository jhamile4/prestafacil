using FluentValidation;
using FluentValidation.Results;

namespace PrestaFacil.Application.Reportes;

// ---------- Respuestas (una fila por resultado de cada procedimiento) ----------

public record CarteraItem(string Estado, int CantidadPrestamos, decimal MontoTotal, decimal SaldoCapitalPendiente);

public record MorosoItem(
    string Dni,
    string Cliente,
    string? Telefono,
    string Prestamo,
    string Asesor,
    int CuotasVencidas,
    int DiasAtraso,
    decimal DeudaVencida,
    decimal MoraPendiente,
    decimal TotalAPagar);

public record CobranzaItem(
    string Cajero,
    string MedioPago,
    int CantidadPagos,
    decimal TotalCobrado,
    decimal Capital,
    decimal Interes,
    decimal Mora);

/// <summary>Ejecuta los procedimientos almacenados de /database/02_procedures.sql.</summary>
public interface IReporteQueries
{
    Task<IReadOnlyList<CarteraItem>> CarteraAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MorosoItem>> MorososAsync(int diasMinimos, DateOnly fechaCorte, CancellationToken ct = default);
    Task<IReadOnlyList<CobranzaItem>> CobranzaAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    /// <summary>sp_ActualizarCuotasVencidas. Devuelve cuántas cuotas actualizó.</summary>
    Task<int> ActualizarCuotasVencidasAsync(DateOnly fechaCorte, CancellationToken ct = default);
}

public record ProcesoMoraResponse(DateOnly FechaCorte, int CuotasActualizadas);

public interface IReporteService
{
    Task<IReadOnlyList<CarteraItem>> CarteraAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MorosoItem>> MorososAsync(int diasMinimos, CancellationToken ct = default);
    Task<IReadOnlyList<CobranzaItem>> CobranzaAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);
    Task<ProcesoMoraResponse> ProcesarMoraAsync(CancellationToken ct = default);
}

public class ReporteService(IReporteQueries queries, TimeProvider reloj) : IReporteService
{
    private DateOnly Hoy => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    /// <summary>RF-16</summary>
    public Task<IReadOnlyList<CarteraItem>> CarteraAsync(CancellationToken ct = default) =>
        queries.CarteraAsync(ct);

    /// <summary>RF-17</summary>
    public Task<IReadOnlyList<MorosoItem>> MorososAsync(int diasMinimos, CancellationToken ct = default)
    {
        if (diasMinimos < 1)
            throw new ValidationException([new ValidationFailure("diasMinimos", "Debe ser al menos 1 día.")]);
        return queries.MorososAsync(diasMinimos, Hoy, ct);
    }

    /// <summary>RF-18</summary>
    public Task<IReadOnlyList<CobranzaItem>> CobranzaAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        if (hasta < desde)
            throw new ValidationException([new ValidationFailure("hasta", "La fecha final no puede ser menor que la inicial.")]);
        if (hasta.DayNumber - desde.DayNumber > 366)
            throw new ValidationException([new ValidationFailure("hasta", "El rango máximo es de un año.")]);
        return queries.CobranzaAsync(desde, hasta, ct);
    }

    /// <summary>RF-14: ejecuta a demanda el proceso diario de mora.</summary>
    public async Task<ProcesoMoraResponse> ProcesarMoraAsync(CancellationToken ct = default)
    {
        var hoy = Hoy;
        return new ProcesoMoraResponse(hoy, await queries.ActualizarCuotasVencidasAsync(hoy, ct));
    }
}
