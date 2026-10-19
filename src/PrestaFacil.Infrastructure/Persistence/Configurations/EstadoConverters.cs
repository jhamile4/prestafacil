using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Infrastructure.Persistence.Configurations;

/// <summary>
/// En la BD los estados se guardan en mayúsculas ('PENDIENTE', 'VENCIDA')
/// por los CHECK constraints; en C# son enums (Pendiente, Vencida).
/// </summary>
internal static class EstadoConverters
{
    public static readonly ValueConverter<EstadoPrestamo, string> Prestamo = new(
        v => v.ToString().ToUpperInvariant(),
        v => Enum.Parse<EstadoPrestamo>(v, true));

    public static readonly ValueConverter<EstadoCuota, string> Cuota = new(
        v => v.ToString().ToUpperInvariant(),
        v => Enum.Parse<EstadoCuota>(v, true));
}
