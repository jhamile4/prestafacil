using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Usuarios;

/// <summary>Hash de contraseñas (implementado con BCrypt en Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verificar(string password, string hash);
}

public record TokenGenerado(string Token, DateTime ExpiraEn);

/// <summary>Emite el JWT (implementado en la API).</summary>
public interface ITokenService
{
    TokenGenerado Generar(Usuario usuario);
}

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default);
    Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default);
    void Agregar(Usuario usuario);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

/// <summary>Nombres de rol tal como viajan en el token y se usan en [Authorize(Roles = ...)].</summary>
public static class Roles
{
    public const string Admin = "ADMIN";
    public const string Asesor = "ASESOR";
    public const string Cajero = "CAJERO";

    public static string Nombre(RolUsuario rol) => rol.ToString().ToUpperInvariant();
}
