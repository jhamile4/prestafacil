using Microsoft.EntityFrameworkCore;
using PrestaFacil.Application.Usuarios;
using PrestaFacil.Domain.Entities;
using PrestaFacil.Infrastructure.Persistence;

namespace PrestaFacil.Infrastructure.Repositories;

public class UsuarioRepository(PrestaFacilDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, ct);

    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario, ct);

    public Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.Email == email && u.Id != excluirId, ct);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) =>
        await db.Usuarios.AsNoTracking().OrderBy(u => u.Rol).ThenBy(u => u.NombreCompleto).ToListAsync(ct);

    public void Agregar(Usuario usuario) => db.Usuarios.Add(usuario);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
