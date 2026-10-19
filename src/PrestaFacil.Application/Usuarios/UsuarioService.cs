using FluentValidation;
using PrestaFacil.Application.Common;
using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Usuarios;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioResponse>> ListarAsync(CancellationToken ct = default);
    Task<UsuarioResponse> ObtenerAsync(int id, CancellationToken ct = default);
    Task<UsuarioResponse> CrearAsync(CrearUsuarioRequest request, CancellationToken ct = default);
    Task<UsuarioResponse> ActualizarAsync(int id, ActualizarUsuarioRequest request, CancellationToken ct = default);
    Task DesactivarAsync(int id, CancellationToken ct = default);
    Task ActivarAsync(int id, CancellationToken ct = default);
    Task RestablecerPasswordAsync(int id, RestablecerPasswordRequest request, CancellationToken ct = default);
}

/// <summary>RF-21: el ADMIN gestiona los usuarios.</summary>
public class UsuarioService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    IValidator<CrearUsuarioRequest> validadorCrear,
    IValidator<ActualizarUsuarioRequest> validadorActualizar,
    IValidator<RestablecerPasswordRequest> validadorPassword,
    ICurrentUser usuarioActual) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioResponse>> ListarAsync(CancellationToken ct = default) =>
        (await usuarios.ListarAsync(ct)).Select(UsuarioResponse.Desde).ToList();

    public async Task<UsuarioResponse> ObtenerAsync(int id, CancellationToken ct = default) =>
        UsuarioResponse.Desde(await ObtenerEntidadAsync(id, ct));

    public async Task<UsuarioResponse> CrearAsync(CrearUsuarioRequest request, CancellationToken ct = default)
    {
        await validadorCrear.ValidateAndThrowAsync(request, ct);

        var nombreUsuario = request.NombreUsuario.Trim().ToLowerInvariant();
        if (await usuarios.ExisteNombreUsuarioAsync(nombreUsuario, ct))
            throw new ConflictException($"Ya existe el usuario '{nombreUsuario}'.");
        if (await usuarios.ExisteEmailAsync(request.Email.Trim().ToLowerInvariant(), null, ct))
            throw new ConflictException($"Ya existe un usuario con el correo {request.Email}.");

        var usuario = new Usuario(nombreUsuario, request.NombreCompleto, request.Email, request.Rol,
            hasher.Hash(request.Password));
        usuarios.Agregar(usuario);
        await usuarios.GuardarCambiosAsync(ct);

        return UsuarioResponse.Desde(usuario);
    }

    public async Task<UsuarioResponse> ActualizarAsync(int id, ActualizarUsuarioRequest request, CancellationToken ct = default)
    {
        await validadorActualizar.ValidateAndThrowAsync(request, ct);

        var usuario = await ObtenerEntidadAsync(id, ct);
        if (id == usuarioActual.UserId && request.Rol != RolUsuario.Admin)
            throw new DomainException("No puede quitarse a sí mismo el rol de administrador.");
        if (await usuarios.ExisteEmailAsync(request.Email.Trim().ToLowerInvariant(), id, ct))
            throw new ConflictException($"Ya existe un usuario con el correo {request.Email}.");

        usuario.Actualizar(request.NombreCompleto, request.Email, request.Rol);
        await usuarios.GuardarCambiosAsync(ct);
        return UsuarioResponse.Desde(usuario);
    }

    public async Task DesactivarAsync(int id, CancellationToken ct = default)
    {
        if (id == usuarioActual.UserId)
            throw new DomainException("No puede desactivar su propio usuario.");

        var usuario = await ObtenerEntidadAsync(id, ct);
        usuario.Desactivar();
        await usuarios.GuardarCambiosAsync(ct);
    }

    public async Task ActivarAsync(int id, CancellationToken ct = default)
    {
        var usuario = await ObtenerEntidadAsync(id, ct);
        usuario.Activar();
        await usuarios.GuardarCambiosAsync(ct);
    }

    public async Task RestablecerPasswordAsync(int id, RestablecerPasswordRequest request, CancellationToken ct = default)
    {
        await validadorPassword.ValidateAndThrowAsync(request, ct);

        var usuario = await ObtenerEntidadAsync(id, ct);
        usuario.CambiarPasswordHash(hasher.Hash(request.PasswordNueva));
        await usuarios.GuardarCambiosAsync(ct);
    }

    private async Task<Usuario> ObtenerEntidadAsync(int id, CancellationToken ct) =>
        await usuarios.ObtenerPorIdAsync(id, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {id}.");
}
