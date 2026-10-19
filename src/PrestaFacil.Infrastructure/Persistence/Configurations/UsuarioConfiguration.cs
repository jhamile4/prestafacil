using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.NombreUsuario).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.HasIndex(u => u.NombreUsuario).IsUnique();
        builder.Property(u => u.NombreCompleto).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsUnicode(false).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
        builder.Property(u => u.Rol).HasConversion(EstadoConverters.Rol).HasMaxLength(10).IsUnicode(false);

        builder.Property(u => u.FechaCreacion)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSDATETIME()")
            .ValueGeneratedOnAdd();
    }
}
