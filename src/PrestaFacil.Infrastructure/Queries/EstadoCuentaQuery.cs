using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrestaFacil.Application.Clientes;
using PrestaFacil.Infrastructure.Persistence;

namespace PrestaFacil.Infrastructure.Queries;

/// <summary>
/// Ejecuta sp_EstadoCuentaCliente con ADO.NET porque devuelve dos
/// resultados (préstamos y cuotas), algo que EF Core no lee directamente.
/// </summary>
public class EstadoCuentaQuery(PrestaFacilDbContext db) : IEstadoCuentaQuery
{
    public async Task<(IReadOnlyList<PrestamoResumen> Prestamos, IReadOnlyList<CuotaPorPagar> Cuotas)> ObtenerAsync(
        int clienteId, CancellationToken ct = default)
    {
        var conexion = db.Database.GetDbConnection();
        var abrioConexion = conexion.State != ConnectionState.Open;
        if (abrioConexion) await conexion.OpenAsync(ct);

        try
        {
            await using var cmd = conexion.CreateCommand();
            cmd.CommandText = "dbo.sp_EstadoCuentaCliente";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@ClienteId", SqlDbType.Int) { Value = clienteId });

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            // Resultado 1: resumen por préstamo
            var prestamos = new List<PrestamoResumen>();
            while (await reader.ReadAsync(ct))
            {
                prestamos.Add(new PrestamoResumen(
                    Codigo: reader.GetString("Codigo"),
                    Producto: reader.GetString("Producto"),
                    Monto: reader.GetDecimal("Monto"),
                    PlazoMeses: reader.GetByte("PlazoMeses"),
                    Tea: reader.GetDecimal("Tea"),
                    CuotaMensual: reader.GetDecimal("CuotaMensual"),
                    Estado: reader.GetString("Estado"),
                    FechaDesembolso: LeerFechaOpcional(reader, "FechaDesembolso"),
                    TotalCuotas: reader.GetInt32("TotalCuotas"),
                    CuotasPagadas: reader.GetInt32("CuotasPagadas"),
                    CuotasVencidas: reader.GetInt32("CuotasVencidas"),
                    SaldoCapital: reader.GetDecimal("SaldoCapital"),
                    DeudaTotal: reader.GetDecimal("DeudaTotal")));
            }

            // Resultado 2: cuotas por pagar
            var cuotas = new List<CuotaPorPagar>();
            if (await reader.NextResultAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    cuotas.Add(new CuotaPorPagar(
                        Prestamo: reader.GetString("Prestamo"),
                        NumeroCuota: reader.GetByte("NumeroCuota"),
                        FechaVencimiento: DateOnly.FromDateTime(reader.GetDateTime("FechaVencimiento")),
                        MontoCuota: reader.GetDecimal("MontoCuota"),
                        Mora: reader.GetDecimal("Mora"),
                        Pendiente: reader.GetDecimal("Pendiente"),
                        Estado: reader.GetString("Estado")));
                }
            }

            return (prestamos, cuotas);
        }
        finally
        {
            if (abrioConexion) await conexion.CloseAsync();
        }
    }

    private static DateOnly? LeerFechaOpcional(DbDataReader reader, string columna)
    {
        var i = reader.GetOrdinal(columna);
        return reader.IsDBNull(i) ? null : DateOnly.FromDateTime(reader.GetDateTime(i));
    }
}
