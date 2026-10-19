using Microsoft.EntityFrameworkCore;
using PrestaFacil.Application.Clientes;
using PrestaFacil.Domain.Entities;
using PrestaFacil.Infrastructure.Persistence;

namespace PrestaFacil.Infrastructure.Repositories;

public class ClienteRepository(PrestaFacilDbContext db) : IClienteRepository
{
    // Ignora mayúsculas y tildes: "ramirez" encuentra a "Ramírez"
    private const string CollationBusqueda = "Latin1_General_CI_AI";

    public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cliente?> ObtenerPorDniAsync(string dni, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Dni == dni, ct);

    public Task<bool> ExisteDniAsync(string dni, CancellationToken ct = default) =>
        db.Clientes.AnyAsync(c => c.Dni == dni, ct);

    public async Task<(IReadOnlyList<Cliente> Items, int Total)> BuscarAsync(
        string? texto, bool soloActivos, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        var query = db.Clientes.AsNoTracking();

        if (soloActivos)
            query = query.Where(c => c.Activo);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            query = texto.All(char.IsAsciiDigit)
                ? query.Where(c => c.Dni.StartsWith(texto))
                : query.Where(c =>
                    EF.Functions.Collate(c.Nombres + " " + c.Apellidos, CollationBusqueda).Contains(texto) ||
                    EF.Functions.Collate(c.Apellidos + " " + c.Nombres, CollationBusqueda).Contains(texto));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Apellidos).ThenBy(c => c.Nombres)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(ct);

        return (items, total);
    }

    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
