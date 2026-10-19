using FluentValidation;
using PrestaFacil.Application.Clientes;
using PrestaFacil.Application.Common;
using PrestaFacil.Domain.Common;

namespace PrestaFacil.UnitTests.Clientes;

public class ClienteServiceTests
{
    private const int UsuarioAsesorId = 2;

    private readonly FakeClienteRepository _repositorio = new();
    private readonly FakeEstadoCuentaQuery _estadoCuenta = new();
    private readonly ClienteService _servicio;

    public ClienteServiceTests()
    {
        _servicio = new ClienteService(
            _repositorio,
            _estadoCuenta,
            new CrearClienteValidator(),
            new ActualizarClienteValidator(),
            new FakeCurrentUser(UsuarioAsesorId),
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)));
    }

    private static CrearClienteRequest SolicitudValida(string dni = "45879632") => new(
        Dni: dni,
        Nombres: "Ana María",
        Apellidos: "Flores Gutiérrez",
        FechaNacimiento: new DateOnly(1990, 3, 14),
        Telefono: "987654321",
        Email: "ana.flores@gmail.com",
        Direccion: "Av. Los Olivos 245",
        IngresoMensual: 4500m,
        GastosMensuales: 1800m);

    [Fact]
    public async Task Crear_ConDatosValidos_GuardaYRegistraQuienLoCreo()
    {
        var respuesta = await _servicio.CrearAsync(SolicitudValida());

        Assert.Equal(1, respuesta.Id);
        Assert.Equal("Flores Gutiérrez, Ana María", respuesta.NombreCompleto);
        Assert.Equal(36, respuesta.Edad);
        Assert.Equal(2700m, respuesta.IngresoNeto);
        Assert.Equal(UsuarioAsesorId, _repositorio.Clientes.Single().CreadoPor);
        Assert.Equal(1, _repositorio.VecesGuardado);
    }

    [Fact]
    public async Task Crear_ConDniRepetido_LanzaConflict()
    {
        await _servicio.CrearAsync(SolicitudValida());

        await Assert.ThrowsAsync<ConflictException>(() => _servicio.CrearAsync(SolicitudValida()));
        Assert.Single(_repositorio.Clientes);
    }

    [Fact]
    public async Task Crear_ConVariosErrores_LosDevuelveTodosJuntos()
    {
        var solicitud = SolicitudValida() with
        {
            Dni = "123",
            Telefono = "12345",
            Email = "no-es-correo",
            IngresoMensual = 0
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _servicio.CrearAsync(solicitud));

        var campos = ex.Errors.Select(e => e.PropertyName).Order().ToArray();
        Assert.Equal(["Dni", "Email", "IngresoMensual", "Telefono"], campos);
        Assert.Empty(_repositorio.Clientes);
    }

    [Fact]
    public async Task Crear_MenorDeEdad_LanzaDomainException()
    {
        var solicitud = SolicitudValida() with { FechaNacimiento = new DateOnly(2010, 1, 1) };

        await Assert.ThrowsAsync<DomainException>(() => _servicio.CrearAsync(solicitud));
        Assert.Empty(_repositorio.Clientes);
    }

    [Fact]
    public async Task ObtenerPorId_Inexistente_LanzaNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _servicio.ObtenerPorIdAsync(99));
    }

    [Fact]
    public async Task ObtenerPorDni_Existente_DevuelveElCliente()
    {
        await _servicio.CrearAsync(SolicitudValida());

        var cliente = await _servicio.ObtenerPorDniAsync(" 45879632 ");

        Assert.Equal("45879632", cliente.Dni);
    }

    [Fact]
    public async Task Actualizar_CambiaLosDatos()
    {
        var creado = await _servicio.CrearAsync(SolicitudValida());
        var cambios = new ActualizarClienteRequest(
            "Ana", "Flores", new DateOnly(1990, 3, 14), null, null, null, 6000m, 2000m);

        var actualizado = await _servicio.ActualizarAsync(creado.Id, cambios);

        Assert.Equal("Ana", actualizado.Nombres);
        Assert.Equal(4000m, actualizado.IngresoNeto);
        Assert.Equal(creado.Dni, actualizado.Dni);
    }

    [Fact]
    public async Task Desactivar_ElClienteYaNoApareceEnLaBusquedaPorDefecto()
    {
        var creado = await _servicio.CrearAsync(SolicitudValida());

        await _servicio.DesactivarAsync(creado.Id);

        var activos = await _servicio.BuscarAsync(new BuscarClientesQuery());
        var todos = await _servicio.BuscarAsync(new BuscarClientesQuery(SoloActivos: false));
        Assert.Equal(0, activos.Total);
        Assert.Equal(1, todos.Total);
        Assert.False(todos.Items.Single().Activo);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]       // página 0 se corrige a 1
    [InlineData(2, 500, 2, 100)]     // tamaño máximo 100
    [InlineData(1, 0, 1, 1)]         // tamaño mínimo 1
    public async Task Buscar_CorrigePaginacionFueraDeRango(
        int pagina, int tamano, int paginaEsperada, int tamanoEsperado)
    {
        var resultado = await _servicio.BuscarAsync(new BuscarClientesQuery(Pagina: pagina, TamanoPagina: tamano));

        Assert.Equal(paginaEsperada, resultado.Pagina);
        Assert.Equal(tamanoEsperado, resultado.TamanoPagina);
    }

    [Fact]
    public async Task EstadoCuenta_SumaLaDeudaDeTodosLosPrestamos()
    {
        var creado = await _servicio.CrearAsync(SolicitudValida());
        _estadoCuenta.Prestamos.Add(Resumen("PR-000001", deuda: 1953.58m));
        _estadoCuenta.Prestamos.Add(Resumen("PR-000008", deuda: 1063.55m));

        var estado = await _servicio.ObtenerEstadoCuentaAsync(creado.Id);

        Assert.Equal(3017.13m, estado.DeudaTotal);
    }

    private static PrestamoResumen Resumen(string codigo, decimal deuda) => new(
        codigo, "Préstamo Personal", 5000m, 12, 0.35m, 488.39m, "DESEMBOLSADO",
        new DateOnly(2026, 2, 19), 12, 8, 0, deuda, deuda);
}
