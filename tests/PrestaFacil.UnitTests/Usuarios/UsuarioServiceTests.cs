using FluentValidation;
using PrestaFacil.Application.Common;
using PrestaFacil.Application.Usuarios;
using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Entities;
using PrestaFacil.UnitTests.Clientes;

namespace PrestaFacil.UnitTests.Usuarios;

public class UsuarioServiceTests
{
    private readonly FakeUsuarioRepository _repositorio = new();
    private readonly Usuario _admin;
    private readonly UsuarioService _servicio;

    public UsuarioServiceTests()
    {
        _admin = _repositorio.Agregar("admin", RolUsuario.Admin, "Admin2026!");
        _servicio = new UsuarioService(
            _repositorio, new FakePasswordHasher(),
            new CrearUsuarioValidator(), new ActualizarUsuarioValidator(), new RestablecerPasswordValidator(),
            new FakeCurrentUser(_admin.Id));
    }

    private static CrearUsuarioRequest NuevoAsesor(string usuario = "asesor3", string email = "asesor3@prestafacil.pe") =>
        new(usuario, "Rosa Medina", email, RolUsuario.Asesor, "Inicial2026!");

    [Fact]
    public async Task Crear_GuardaElHashYNoLaPassword()
    {
        var creado = await _servicio.CrearAsync(NuevoAsesor());

        var usuario = _repositorio.Usuarios.Single(u => u.Id == creado.Id);
        Assert.Equal(FakePasswordHasher.HashDe("Inicial2026!"), usuario.PasswordHash);
        Assert.Equal(RolUsuario.Asesor, creado.Rol);
        Assert.True(creado.Activo);
    }

    [Fact]
    public async Task Crear_UsuarioRepetido_LanzaConflict()
    {
        await _servicio.CrearAsync(NuevoAsesor());

        await Assert.ThrowsAsync<ConflictException>(() =>
            _servicio.CrearAsync(NuevoAsesor("ASESOR3", "otro@prestafacil.pe")));
    }

    [Fact]
    public async Task Crear_CorreoRepetido_LanzaConflict()
    {
        await _servicio.CrearAsync(NuevoAsesor());

        await Assert.ThrowsAsync<ConflictException>(() =>
            _servicio.CrearAsync(NuevoAsesor("asesor4", "ASESOR3@prestafacil.pe")));
    }

    [Fact]
    public async Task Crear_ConPasswordDebil_Falla()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _servicio.CrearAsync(NuevoAsesor() with { Password = "123456" }));
    }

    [Fact]
    public async Task Desactivarse_ASiMismo_Falla()
    {
        await Assert.ThrowsAsync<DomainException>(() => _servicio.DesactivarAsync(_admin.Id));
        Assert.True(_admin.Activo);
    }

    [Fact]
    public async Task QuitarseElRolDeAdmin_Falla()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _servicio.ActualizarAsync(_admin.Id, new ActualizarUsuarioRequest("Admin", "admin@prestafacil.pe", RolUsuario.Cajero)));
        Assert.Equal(RolUsuario.Admin, _admin.Rol);
    }

    [Fact]
    public async Task DesactivarOtroUsuario_LeImpideEntrar()
    {
        var creado = await _servicio.CrearAsync(NuevoAsesor());

        await _servicio.DesactivarAsync(creado.Id);

        Assert.False((await _servicio.ObtenerAsync(creado.Id)).Activo);
    }
}
