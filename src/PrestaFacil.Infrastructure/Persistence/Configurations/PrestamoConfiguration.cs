using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Infrastructure.Persistence.Configurations;

public class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
{
    public void Configure(EntityTypeBuilder<Prestamo> builder)
    {
        builder.ToTable("Prestamos");
        builder.HasKey(p => p.Id);

        // Columna calculada en SQL Server: EF la lee después del INSERT y nunca la escribe
        builder.Property(p => p.Codigo)
            .HasMaxLength(9)
            .IsUnicode(false)
            .HasComputedColumnSql("'PR-' + RIGHT('000000' + CAST(Id AS VARCHAR(10)), 6)", stored: true);

        builder.Property(p => p.Monto).HasPrecision(12, 2);
        builder.Property(p => p.PlazoMeses).HasColumnType("tinyint");
        builder.Property(p => p.Tea).HasPrecision(9, 6);
        builder.Property(p => p.Tem).HasPrecision(12, 10);
        builder.Property(p => p.CuotaMensual).HasPrecision(12, 2);
        builder.Property(p => p.TasaMoraDiaria).HasPrecision(9, 6);
        builder.Property(p => p.Estado).HasConversion(EstadoConverters.Prestamo).HasMaxLength(12).IsUnicode(false);
        builder.Property(p => p.FechaSolicitud).HasColumnType("datetime2(0)");
        builder.Property(p => p.FechaAprobacion).HasColumnType("datetime2(0)");
        builder.Property(p => p.MotivoRechazo).HasMaxLength(300);

        builder.HasOne(p => p.Cliente).WithMany().HasForeignKey(p => p.ClienteId);
        builder.HasOne(p => p.Producto).WithMany().HasForeignKey(p => p.ProductoId);

        builder.HasMany(p => p.Cuotas).WithOne().HasForeignKey(c => c.PrestamoId);
        builder.Navigation(p => p.Cuotas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Historial).WithOne().HasForeignKey(h => h.PrestamoId);
        builder.Navigation(p => p.Historial).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class CuotaConfiguration : IEntityTypeConfiguration<Cuota>
{
    public void Configure(EntityTypeBuilder<Cuota> builder)
    {
        builder.ToTable("Cuotas");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NumeroCuota).HasColumnType("tinyint");
        builder.Property(c => c.SaldoInicial).HasPrecision(12, 2);
        builder.Property(c => c.Capital).HasPrecision(12, 2);
        builder.Property(c => c.Interes).HasPrecision(12, 2);
        builder.Property(c => c.Mora).HasPrecision(12, 2);
        builder.Property(c => c.CapitalPagado).HasPrecision(12, 2);
        builder.Property(c => c.InteresPagado).HasPrecision(12, 2);
        builder.Property(c => c.MoraPagada).HasPrecision(12, 2);
        builder.Property(c => c.Estado).HasConversion(EstadoConverters.Cuota).HasMaxLength(10).IsUnicode(false);
        // MontoCuota, TotalPagado y Pendiente se calculan en C#; no se mapean
    }
}

public class HistorialEstadoPrestamoConfiguration : IEntityTypeConfiguration<HistorialEstadoPrestamo>
{
    public void Configure(EntityTypeBuilder<HistorialEstadoPrestamo> builder)
    {
        builder.ToTable("HistorialEstadosPrestamo");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.EstadoAnterior).HasConversion(EstadoConverters.Prestamo).HasMaxLength(12).IsUnicode(false);
        builder.Property(h => h.EstadoNuevo).HasConversion(EstadoConverters.Prestamo).HasMaxLength(12).IsUnicode(false);
        builder.Property(h => h.Fecha).HasColumnType("datetime2(0)");
        builder.Property(h => h.Comentario).HasMaxLength(300);
    }
}
