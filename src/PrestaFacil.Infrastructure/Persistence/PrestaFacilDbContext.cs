using Microsoft.EntityFrameworkCore;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Infrastructure.Persistence;

/// <summary>
/// El esquema lo definen los scripts de /database (database-first).
/// Las configuraciones solo mapean las clases a las tablas existentes,
/// por eso no se usan migraciones de EF Core.
/// </summary>
public class PrestaFacilDbContext(DbContextOptions<PrestaFacilDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ProductoPrestamo> Productos => Set<ProductoPrestamo>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrestaFacilDbContext).Assembly);
    }
}
