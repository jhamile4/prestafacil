using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Prestamos;

/// <summary>Lo que el cliente ya debe, para evaluar una nueva solicitud.</summary>
public record DeudaActualCliente(int PrestamosActivos, decimal CuotasMensuales, bool TieneCuotasVencidas);

public interface IPrestamoRepository
{
    /// <summary>Incluye cliente y producto; con <paramref name="conDetalle"/> también cuotas e historial.</summary>
    Task<Prestamo?> ObtenerAsync(int id, bool conDetalle, CancellationToken ct = default);

    Task<(IReadOnlyList<Prestamo> Items, int Total)> BuscarAsync(
        EstadoPrestamo? estado, int? clienteId, int pagina, int tamanoPagina, CancellationToken ct = default);

    /// <summary>
    /// Préstamos APROBADOS o DESEMBOLSADOS del cliente (RN-04, RN-05) y si tiene
    /// cuotas vencidas a la fecha (RN-06). Excluye el préstamo que se está evaluando.
    /// </summary>
    Task<DeudaActualCliente> ObtenerDeudaActualAsync(
        int clienteId, int? excluirPrestamoId, DateOnly hoy, CancellationToken ct = default);

    void Agregar(Prestamo prestamo);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
