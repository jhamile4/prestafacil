using PrestaFacil.Application.Common;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Productos;

public record ProductoResponse(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal Tea,
    decimal MontoMinimo,
    decimal MontoMaximo,
    int PlazoMinimoMeses,
    int PlazoMaximoMeses,
    decimal TasaMoraDiaria)
{
    public static ProductoResponse Desde(ProductoPrestamo p) => new(
        p.Id, p.Nombre, p.Descripcion, p.Tea, p.MontoMinimo, p.MontoMaximo,
        p.PlazoMinimoMeses, p.PlazoMaximoMeses, p.TasaMoraDiaria);
}

public interface IProductoRepository
{
    Task<IReadOnlyList<ProductoPrestamo>> ListarActivosAsync(CancellationToken ct = default);
    Task<ProductoPrestamo?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
}

public interface IProductoService
{
    Task<IReadOnlyList<ProductoResponse>> ListarActivosAsync(CancellationToken ct = default);
    Task<ProductoResponse> ObtenerAsync(int id, CancellationToken ct = default);
}

public class ProductoService(IProductoRepository repositorio) : IProductoService
{
    public async Task<IReadOnlyList<ProductoResponse>> ListarActivosAsync(CancellationToken ct = default) =>
        (await repositorio.ListarActivosAsync(ct)).Select(ProductoResponse.Desde).ToList();

    public async Task<ProductoResponse> ObtenerAsync(int id, CancellationToken ct = default) =>
        ProductoResponse.Desde(await repositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new NotFoundException($"No existe el producto con id {id}."));
}
