using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Clientes;

/// <summary>Datos comunes a crear y actualizar, para validarlos una sola vez.</summary>
public interface IClienteDatos
{
    string Nombres { get; }
    string Apellidos { get; }
    DateOnly FechaNacimiento { get; }
    string? Telefono { get; }
    string? Email { get; }
    string? Direccion { get; }
    decimal IngresoMensual { get; }
    decimal GastosMensuales { get; }
}

public record CrearClienteRequest(
    string Dni,
    string Nombres,
    string Apellidos,
    DateOnly FechaNacimiento,
    string? Telefono,
    string? Email,
    string? Direccion,
    decimal IngresoMensual,
    decimal GastosMensuales) : IClienteDatos;

public record ActualizarClienteRequest(
    string Nombres,
    string Apellidos,
    DateOnly FechaNacimiento,
    string? Telefono,
    string? Email,
    string? Direccion,
    decimal IngresoMensual,
    decimal GastosMensuales) : IClienteDatos;

public record BuscarClientesQuery(
    string? Buscar = null,
    bool SoloActivos = true,
    int Pagina = 1,
    int TamanoPagina = 20);

public record ClienteResponse(
    int Id,
    string Dni,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    DateOnly FechaNacimiento,
    int Edad,
    string? Telefono,
    string? Email,
    string? Direccion,
    decimal IngresoMensual,
    decimal GastosMensuales,
    decimal IngresoNeto,
    bool Activo,
    DateTime FechaCreacion)
{
    public static ClienteResponse Desde(Cliente c, DateOnly hoy) => new(
        c.Id, c.Dni, c.Nombres, c.Apellidos, $"{c.Apellidos}, {c.Nombres}",
        c.FechaNacimiento, Cliente.CalcularEdad(c.FechaNacimiento, hoy),
        c.Telefono, c.Email, c.Direccion,
        c.IngresoMensual, c.GastosMensuales, c.IngresoNeto,
        c.Activo, c.FechaCreacion);
}

public record EstadoCuentaResponse(
    ClienteResponse Cliente,
    IReadOnlyList<PrestamoResumen> Prestamos,
    IReadOnlyList<CuotaPorPagar> CuotasPorPagar)
{
    public decimal DeudaTotal => Prestamos.Sum(p => p.DeudaTotal);
}

public record PrestamoResumen(
    string Codigo,
    string Producto,
    decimal Monto,
    int PlazoMeses,
    decimal Tea,
    decimal CuotaMensual,
    string Estado,
    DateOnly? FechaDesembolso,
    int TotalCuotas,
    int CuotasPagadas,
    int CuotasVencidas,
    decimal SaldoCapital,
    decimal DeudaTotal);

public record CuotaPorPagar(
    string Prestamo,
    int NumeroCuota,
    DateOnly FechaVencimiento,
    decimal MontoCuota,
    decimal Mora,
    decimal Pendiente,
    string Estado);
