using PrestaFacil.Application.Usuarios;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.UnitTests.Usuarios;

internal class FakeUsuarioRepository : IUsuarioRepository
{
    private int _siguienteId = 1;
    public List<Usuario> Usuarios { get; } = [];

    public Usuario Agregar(string nombreUsuario, RolUsuario rol, string password, bool activo = true)
    {
        var usuario = new Usuario(nombreUsuario, $"Nombre {nombreUsuario}", $"{nombreUsuario}@prestafacil.pe",
            rol, FakePasswordHasher.HashDe(password));
        if (!activo) usuario.Desactivar();
        Agregar(usuario);
        return usuario;
    }

    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario));

    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default) =>
        Task.FromResult(Usuarios.Any(u => u.NombreUsuario == nombreUsuario));

    public Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Usuarios.Any(u => u.Email == email && u.Id != excluirId));

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Usuario>>(Usuarios);

    public void Agregar(Usuario usuario)
    {
        typeof(Usuario).GetProperty(nameof(Usuario.Id))!.SetValue(usuario, _siguienteId++);
        Usuarios.Add(usuario);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Hash falso y rápido: las pruebas no necesitan la lentitud de BCrypt.</summary>
internal class FakePasswordHasher : IPasswordHasher
{
    public int Verificaciones { get; private set; }

    public static string HashDe(string password) => "hash:" + password;

    public string Hash(string password) => HashDe(password);

    public bool Verificar(string password, string hash)
    {
        Verificaciones++;
        return hash == HashDe(password);
    }
}

internal class FakeTokenService : ITokenService
{
    public TokenGenerado Generar(Usuario usuario) =>
        new($"token-de-{usuario.NombreUsuario}", new DateTime(2026, 10, 19, 11, 0, 0));
}
