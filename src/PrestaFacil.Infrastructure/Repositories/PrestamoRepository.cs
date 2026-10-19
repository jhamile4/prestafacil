using Microsoft.EntityFrameworkCore;
using PrestaFacil.Application.Prestamos;
using PrestaFacil.Application.Productos;
using PrestaFacil.Domain.Entities;
using PrestaFacil.Infrastructure.Persistence;

namespace PrestaFacil.Infrastructure.Repositories;

public class PrestamoRepository(PrestaFacilDbContext db) : IPrestamoRepository
{
    public async Task<Prestamo?> ObtenerAsync(int id, bool conDetalle, CancellationToken ct = default)
    {
        var query = db.Prestamos
            .Include(p => p.Cliente)
            .Include(p => p.Producto)
            .AsQueryable();

        if (conDetalle)
            query = query.Include(p => p.Cuotas).Include(p => p.Historial).AsSplitQuery();

        return await query.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<(IReadOnlyList<Prestamo> Items, int Total)> BuscarAsync(
        EstadoPrestamo? estado, int? clienteId, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        var query = db.Prestamos.AsNoTracking();

        if (estado is not null) query = query.Where(p => p.Estado == estado);
        if (clienteId is not null) query = query.Where(p => p.ClienteId == clienteId);

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(p => p.Cliente)
            .Include(p => p.Producto)
            .OrderByDescending(p => p.FechaSolicitud).ThenByDescending(p => p.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<DeudaActualCliente> ObtenerDeudaActualAsync(
        int clienteId, int? excluirPrestamoId, DateOnly hoy, CancellationToken ct = default)
    {
        var vigentes = db.Prestamos.AsNoTracking()
            .Where(p => p.ClienteId == clienteId
                     && p.Id != excluirPrestamoId
                     && (p.Estado == EstadoPrestamo.Aprobado || p.Estado == EstadoPrestamo.Desembolsado));

        var resumen = await vigentes
            .GroupBy(_ => 1)
            .Select(g => new { Cantidad = g.Count(), Cuotas = g.Sum(p => p.CuotaMensual) })
            .FirstOrDefaultAsync(ct);

        // Se calcula con la fecha, no con el estado VENCIDA, para no depender
        // de que el proceso diario de mora ya se haya ejecutado hoy
        var tieneVencidas = await db.Prestamos.AsNoTracking()
            .Where(p => p.ClienteId == clienteId && p.Estado == EstadoPrestamo.Desembolsado)
            .SelectMany(p => p.Cuotas)
            .AnyAsync(c => c.Estado != EstadoCuota.Pagada && c.FechaVencimiento < hoy, ct);

        return new DeudaActualCliente(resumen?.Cantidad ?? 0, resumen?.Cuotas ?? 0m, tieneVencidas);
    }

    public void Agregar(Prestamo prestamo) => db.Prestamos.Add(prestamo);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

public class ProductoRepository(PrestaFacilDbContext db) : IProductoRepository
{
    public async Task<IReadOnlyList<ProductoPrestamo>> ListarActivosAsync(CancellationToken ct = default) =>
        await db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync(ct);

    public Task<ProductoPrestamo?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct);
}
