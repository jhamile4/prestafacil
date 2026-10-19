using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Dni).HasColumnType("char(8)").IsRequired();
        builder.HasIndex(c => c.Dni).IsUnique();

        builder.Property(c => c.Nombres).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Apellidos).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Telefono).HasMaxLength(15).IsUnicode(false);
        builder.Property(c => c.Email).HasMaxLength(150).IsUnicode(false);
        builder.Property(c => c.Direccion).HasMaxLength(200);

        builder.Property(c => c.IngresoMensual).HasPrecision(12, 2);
        builder.Property(c => c.GastosMensuales).HasPrecision(12, 2);

        builder.Property(c => c.FechaCreacion)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSDATETIME()")
            .ValueGeneratedOnAdd();
    }
}
