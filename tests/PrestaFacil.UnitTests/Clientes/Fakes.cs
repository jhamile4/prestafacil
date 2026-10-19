using PrestaFacil.Application.Clientes;
using PrestaFacil.Application.Common;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.UnitTests.Clientes;

/// <summary>Repositorio en memoria: prueba el servicio sin SQL Server.</summary>
internal class FakeClienteRepository : IClienteRepository
{
    private int _siguienteId = 1;
    public List<Cliente> Clientes { get; } = [];
    public int VecesGuardado { get; private set; }

    public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Clientes.FirstOrDefault(c => c.Id == id));

    public Task<Cliente?> ObtenerPorDniAsync(string dni, CancellationToken ct = default) =>
        Task.FromResult(Clientes.FirstOrDefault(c => c.Dni == dni));

    public Task<bool> ExisteDniAsync(string dni, CancellationToken ct = default) =>
        Task.FromResult(Clientes.Any(c => c.Dni == dni));

    public Task<(IReadOnlyList<Cliente> Items, int Total)> BuscarAsync(
        string? texto, bool soloActivos, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        var query = Clientes.Where(c => !soloActivos || c.Activo).ToList();
        IReadOnlyList<Cliente> items = query.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();
        return Task.FromResult((items, query.Count));
    }

    public void Agregar(Cliente cliente)
    {
        // Simula el IDENTITY de SQL Server
        typeof(Cliente).GetProperty(nameof(Cliente.Id))!.SetValue(cliente, _siguienteId++);
        Clientes.Add(cliente);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default)
    {
        VecesGuardado++;
        return Task.CompletedTask;
    }
}

internal class FakeEstadoCuentaQuery : IEstadoCuentaQuery
{
    public List<PrestamoResumen> Prestamos { get; } = [];
    public List<CuotaPorPagar> Cuotas { get; } = [];

    public Task<(IReadOnlyList<PrestamoResumen> Prestamos, IReadOnlyList<CuotaPorPagar> Cuotas)> ObtenerAsync(
        int clienteId, CancellationToken ct = default) =>
        Task.FromResult<(IReadOnlyList<PrestamoResumen>, IReadOnlyList<CuotaPorPagar>)>((Prestamos, Cuotas));
}

internal class FakeCurrentUser(int userId) : ICurrentUser
{
    public int UserId => userId;
}

/// <summary>Reloj fijo para que las pruebas de edad no dependan del día en que se ejecutan.</summary>
internal class FixedTimeProvider(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
