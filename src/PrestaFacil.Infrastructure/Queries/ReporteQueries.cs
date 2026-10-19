using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrestaFacil.Application.Reportes;
using PrestaFacil.Infrastructure.Persistence;

namespace PrestaFacil.Infrastructure.Queries;

/// <summary>Llama a los procedimientos de reportes y mora con ADO.NET.</summary>
public class ReporteQueries(PrestaFacilDbContext db) : IReporteQueries
{
    public Task<IReadOnlyList<CarteraItem>> CarteraAsync(CancellationToken ct = default) =>
        LeerAsync("dbo.sp_ReporteCartera", [], r => new CarteraItem(
            r.GetString("Estado"),
            r.GetInt32("CantidadPrestamos"),
            r.GetDecimal("MontoTotal"),
            r.GetDecimal("SaldoCapitalPendiente")), ct);

    public Task<IReadOnlyList<MorosoItem>> MorososAsync(int diasMinimos, DateOnly fechaCorte, CancellationToken ct = default) =>
        LeerAsync("dbo.sp_ReporteMorosos",
            [Parametro("@DiasMinimos", SqlDbType.Int, diasMinimos), Parametro("@FechaCorte", SqlDbType.Date, fechaCorte)],
            r => new MorosoItem(
                r.GetString("Dni"),
                r.GetString("Cliente"),
                r.IsDBNull("Telefono") ? null : r.GetString("Telefono"),
                r.GetString("Prestamo"),
                r.GetString("Asesor"),
                r.GetInt32("CuotasVencidas"),
                r.GetInt32("DiasAtraso"),
                r.GetDecimal("DeudaVencida"),
                r.GetDecimal("MoraPendiente"),
                r.GetDecimal("TotalAPagar")), ct);

    public Task<IReadOnlyList<CobranzaItem>> CobranzaAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        LeerAsync("dbo.sp_ReporteCobranza",
            [Parametro("@Desde", SqlDbType.Date, desde), Parametro("@Hasta", SqlDbType.Date, hasta)],
            r => new CobranzaItem(
                r.GetString("Cajero"),
                r.GetString("MedioPago"),
                r.GetInt32("CantidadPagos"),
                r.GetDecimal("TotalCobrado"),
                DecimalONulo(r, "Capital"),
                DecimalONulo(r, "Interes"),
                DecimalONulo(r, "Mora")), ct);

    public async Task<int> ActualizarCuotasVencidasAsync(DateOnly fechaCorte, CancellationToken ct = default)
    {
        var filas = await LeerAsync("dbo.sp_ActualizarCuotasVencidas",
            [Parametro("@FechaCorte", SqlDbType.Date, fechaCorte)],
            r => r.GetInt32("CuotasActualizadas"), ct);
        return filas.Single();
    }

    private async Task<IReadOnlyList<T>> LeerAsync<T>(
        string procedimiento, SqlParameter[] parametros, Func<DbDataReader, T> mapear, CancellationToken ct)
    {
        var conexion = db.Database.GetDbConnection();
        var abrioConexion = conexion.State != ConnectionState.Open;
        if (abrioConexion) await conexion.OpenAsync(ct);

        try
        {
            await using var cmd = conexion.CreateCommand();
            cmd.CommandText = procedimiento;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parametros);

            var resultado = new List<T>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                resultado.Add(mapear(reader));
            return resultado;
        }
        finally
        {
            if (abrioConexion) await conexion.CloseAsync();
        }
    }

    private static SqlParameter Parametro(string nombre, SqlDbType tipo, object valor) =>
        new(nombre, tipo) { Value = valor is DateOnly fecha ? fecha.ToDateTime(TimeOnly.MinValue) : valor };

    private static decimal DecimalONulo(DbDataReader r, string columna) =>
        r.IsDBNull(columna) ? 0m : r.GetDecimal(columna);
}
