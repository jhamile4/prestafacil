using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Clientes;

public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Cliente?> ObtenerPorDniAsync(string dni, CancellationToken ct = default);
    Task<bool> ExisteDniAsync(string dni, CancellationToken ct = default);

    /// <summary>Busca por DNI (prefijo) o por nombres/apellidos (contiene).</summary>
    Task<(IReadOnlyList<Cliente> Items, int Total)> BuscarAsync(
        string? texto, bool soloActivos, int pagina, int tamanoPagina, CancellationToken ct = default);

    void Agregar(Cliente cliente);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

/// <summary>Lectura del estado de cuenta (sp_EstadoCuentaCliente).</summary>
public interface IEstadoCuentaQuery
{
    Task<(IReadOnlyList<PrestamoResumen> Prestamos, IReadOnlyList<CuotaPorPagar> Cuotas)> ObtenerAsync(
        int clienteId, CancellationToken ct = default);
}
