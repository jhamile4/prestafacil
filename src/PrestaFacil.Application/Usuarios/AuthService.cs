using FluentValidation;
using PrestaFacil.Application.Common;

namespace PrestaFacil.Application.Usuarios;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UsuarioResponse> ObtenerUsuarioActualAsync(CancellationToken ct = default);
    Task CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken ct = default);
}

/// <summary>RF-19: inicio de sesión con usuario y contraseña; devuelve un JWT.</summary>
public class AuthService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    ITokenService tokens,
    IValidator<LoginRequest> validadorLogin,
    IValidator<CambiarPasswordRequest> validadorCambio,
    ICurrentUser usuarioActual) : IAuthService
{
    // Mismo mensaje para usuario inexistente, contraseña incorrecta o usuario
    // desactivado: así no se revela qué usuarios existen
    private const string CredencialesInvalidas = "Usuario o contraseña incorrectos.";

    private static string? _hashFicticio;
    private string HashFicticio() => _hashFicticio ??= hasher.Hash(Guid.NewGuid().ToString());

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await validadorLogin.ValidateAndThrowAsync(request, ct);

        var usuario = await usuarios.ObtenerPorNombreUsuarioAsync(request.NombreUsuario.Trim().ToLowerInvariant(), ct);

        // Si el usuario no existe igual se verifica contra un hash ficticio, para que
        // la respuesta tarde lo mismo y no se pueda adivinar qué usuarios existen
        var passwordCorrecta = hasher.Verificar(request.Password, usuario?.PasswordHash ?? HashFicticio());
        if (usuario is null || !usuario.Activo || !passwordCorrecta)
            throw new UnauthorizedException(CredencialesInvalidas);

        var token = tokens.Generar(usuario);
        return new LoginResponse(token.Token, "Bearer", token.ExpiraEn, UsuarioResponse.Desde(usuario));
    }

    public async Task<UsuarioResponse> ObtenerUsuarioActualAsync(CancellationToken ct = default) =>
        UsuarioResponse.Desde(await ObtenerActualAsync(ct));

    public async Task CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken ct = default)
    {
        await validadorCambio.ValidateAndThrowAsync(request, ct);

        var usuario = await ObtenerActualAsync(ct);
        if (!hasher.Verificar(request.PasswordActual, usuario.PasswordHash))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.PasswordActual), "La contraseña actual no es correcta.")]);

        usuario.CambiarPasswordHash(hasher.Hash(request.PasswordNueva));
        await usuarios.GuardarCambiosAsync(ct);
    }

    private async Task<Domain.Entities.Usuario> ObtenerActualAsync(CancellationToken ct) =>
        await usuarios.ObtenerPorIdAsync(usuarioActual.UserId, ct)
            ?? throw new UnauthorizedException("La sesión no es válida.");
}
