using Microsoft.AspNetCore.Mvc;
using PrestaFacil.Application.Productos;

namespace PrestaFacil.Api.Controllers;

/// <summary>Productos de préstamo disponibles (RF-05).</summary>
[ApiController]
[Route("api/productos")]
[Produces("application/json")]
public class ProductosController(IProductoService productos) : ControllerBase
{
    /// <summary>Lista los productos activos con sus tasas y límites.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductoResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ProductoResponse>> Listar(CancellationToken ct) =>
        productos.ListarActivosAsync(ct);

    /// <summary>Obtiene un producto por su id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ProductoResponse> Obtener(int id, CancellationToken ct) =>
        productos.ObtenerAsync(id, ct);
}
