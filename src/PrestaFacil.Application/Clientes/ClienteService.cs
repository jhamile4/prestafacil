using FluentValidation;
using PrestaFacil.Application.Common;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Clientes;

public interface IClienteService
{
    Task<PagedResult<ClienteResponse>> BuscarAsync(BuscarClientesQuery query, CancellationToken ct = default);
    Task<ClienteResponse> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<ClienteResponse> ObtenerPorDniAsync(string dni, CancellationToken ct = default);
    Task<ClienteResponse> CrearAsync(CrearClienteRequest request, CancellationToken ct = default);
    Task<ClienteResponse> ActualizarAsync(int id, ActualizarClienteRequest request, CancellationToken ct = default);
    Task DesactivarAsync(int id, CancellationToken ct = default);
    Task ActivarAsync(int id, CancellationToken ct = default);
    Task<EstadoCuentaResponse> ObtenerEstadoCuentaAsync(int id, CancellationToken ct = default);
}

public class ClienteService(
    IClienteRepository repositorio,
    IEstadoCuentaQuery estadoCuentaQuery,
    IValidator<CrearClienteRequest> validadorCrear,
    IValidator<ActualizarClienteRequest> validadorActualizar,
    ICurrentUser usuarioActual,
    TimeProvider reloj) : IClienteService
{
    public const int TamanoPaginaMaximo = 100;

    private DateOnly Hoy => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    public async Task<PagedResult<ClienteResponse>> BuscarAsync(BuscarClientesQuery query, CancellationToken ct = default)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamano = Math.Clamp(query.TamanoPagina, 1, TamanoPaginaMaximo);

        var (items, total) = await repositorio.BuscarAsync(query.Buscar?.Trim(), query.SoloActivos, pagina, tamano, ct);

        var hoy = Hoy;
        return new PagedResult<ClienteResponse>(
            items.Select(c => ClienteResponse.Desde(c, hoy)).ToList(), total, pagina, tamano);
    }

    public async Task<ClienteResponse> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        ClienteResponse.Desde(await ObtenerEntidadAsync(id, ct), Hoy);

    public async Task<ClienteResponse> ObtenerPorDniAsync(string dni, CancellationToken ct = default)
    {
        var cliente = await repositorio.ObtenerPorDniAsync(dni.Trim(), ct)
            ?? throw new NotFoundException($"No existe un cliente con DNI {dni}.");
        return ClienteResponse.Desde(cliente, Hoy);
    }

    public async Task<ClienteResponse> CrearAsync(CrearClienteRequest request, CancellationToken ct = default)
    {
        await validadorCrear.ValidateAndThrowAsync(request, ct);

        // RN-01: el DNI es único
        if (await repositorio.ExisteDniAsync(request.Dni.Trim(), ct))
            throw new ConflictException($"Ya existe un cliente con DNI {request.Dni}.");

        var cliente = new Cliente(
            request.Dni, request.Nombres, request.Apellidos, request.FechaNacimiento,
            request.Telefono, request.Email, request.Direccion,
            request.IngresoMensual, request.GastosMensuales,
            usuarioActual.UserId, Hoy);

        repositorio.Agregar(cliente);
        await repositorio.GuardarCambiosAsync(ct);

        return ClienteResponse.Desde(cliente, Hoy);
    }

    public async Task<ClienteResponse> ActualizarAsync(int id, ActualizarClienteRequest request, CancellationToken ct = default)
    {
        await validadorActualizar.ValidateAndThrowAsync(request, ct);

        var cliente = await ObtenerEntidadAsync(id, ct);
        cliente.Actualizar(
            request.Nombres, request.Apellidos, request.FechaNacimiento,
            request.Telefono, request.Email, request.Direccion,
            request.IngresoMensual, request.GastosMensuales, Hoy);

        await repositorio.GuardarCambiosAsync(ct);
        return ClienteResponse.Desde(cliente, Hoy);
    }

    public async Task DesactivarAsync(int id, CancellationToken ct = default)
    {
        var cliente = await ObtenerEntidadAsync(id, ct);
        cliente.Desactivar();
        await repositorio.GuardarCambiosAsync(ct);
    }

    public async Task ActivarAsync(int id, CancellationToken ct = default)
    {
        var cliente = await ObtenerEntidadAsync(id, ct);
        cliente.Activar();
        await repositorio.GuardarCambiosAsync(ct);
    }

    public async Task<EstadoCuentaResponse> ObtenerEstadoCuentaAsync(int id, CancellationToken ct = default)
    {
        var cliente = await ObtenerEntidadAsync(id, ct);
        var (prestamos, cuotas) = await estadoCuentaQuery.ObtenerAsync(id, ct);
        return new EstadoCuentaResponse(ClienteResponse.Desde(cliente, Hoy), prestamos, cuotas);
    }

    private async Task<Cliente> ObtenerEntidadAsync(int id, CancellationToken ct) =>
        await repositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new NotFoundException($"No existe el cliente con id {id}.");
}
