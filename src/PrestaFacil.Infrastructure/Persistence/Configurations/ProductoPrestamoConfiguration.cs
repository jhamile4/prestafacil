using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Infrastructure.Persistence.Configurations;

public class ProductoPrestamoConfiguration : IEntityTypeConfiguration<ProductoPrestamo>
{
    public void Configure(EntityTypeBuilder<ProductoPrestamo> builder)
    {
        builder.ToTable("ProductosPrestamo");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Descripcion).HasMaxLength(300);
        builder.Property(p => p.Tea).HasPrecision(9, 6);
        builder.Property(p => p.TasaMoraDiaria).HasPrecision(9, 6);
        builder.Property(p => p.MontoMinimo).HasPrecision(12, 2);
        builder.Property(p => p.MontoMaximo).HasPrecision(12, 2);
        builder.Property(p => p.PlazoMinimoMeses).HasColumnType("tinyint");
        builder.Property(p => p.PlazoMaximoMeses).HasColumnType("tinyint");
    }
}
