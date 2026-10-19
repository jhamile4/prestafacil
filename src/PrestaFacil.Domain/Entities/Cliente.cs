using PrestaFacil.Domain.Common;

namespace PrestaFacil.Domain.Entities;

public class Cliente
{
    public const int EdadMinima = 18;

    public int Id { get; private set; }
    public string Dni { get; private set; } = null!;
    public string Nombres { get; private set; } = null!;
    public string Apellidos { get; private set; } = null!;
    public DateOnly FechaNacimiento { get; private set; }
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public string? Direccion { get; private set; }
    public decimal IngresoMensual { get; private set; }
    public decimal GastosMensuales { get; private set; }
    public bool Activo { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public int CreadoPor { get; private set; }

    /// <summary>Ingreso disponible para pagar deudas (base de RN-04).</summary>
    public decimal IngresoNeto => IngresoMensual - GastosMensuales;

    // Requerido por EF Core
    private Cliente() { }

    public Cliente(
        string dni,
        string nombres,
        string apellidos,
        DateOnly fechaNacimiento,
        string? telefono,
        string? email,
        string? direccion,
        decimal ingresoMensual,
        decimal gastosMensuales,
        int creadoPor,
        DateOnly hoy)
    {
        Dni = ValidarDni(dni);
        CreadoPor = creadoPor;
        Activo = true;
        // FechaCreacion la asigna la base de datos (DEFAULT SYSDATETIME())
        AsignarDatos(nombres, apellidos, fechaNacimiento, telefono, email, direccion,
                     ingresoMensual, gastosMensuales, hoy);
    }

    /// <summary>Actualiza los datos editables. El DNI no se puede cambiar.</summary>
    public void Actualizar(
        string nombres,
        string apellidos,
        DateOnly fechaNacimiento,
        string? telefono,
        string? email,
        string? direccion,
        decimal ingresoMensual,
        decimal gastosMensuales,
        DateOnly hoy)
    {
        AsignarDatos(nombres, apellidos, fechaNacimiento, telefono, email, direccion,
                     ingresoMensual, gastosMensuales, hoy);
    }

    /// <summary>RN-11: los clientes no se eliminan, se desactivan.</summary>
    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;

    public static int CalcularEdad(DateOnly fechaNacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - fechaNacimiento.Year;
        // Todavía no cumple años este año
        if (fechaNacimiento > hoy.AddYears(-edad)) edad--;
        return edad;
    }

    private void AsignarDatos(
        string nombres,
        string apellidos,
        DateOnly fechaNacimiento,
        string? telefono,
        string? email,
        string? direccion,
        decimal ingresoMensual,
        decimal gastosMensuales,
        DateOnly hoy)
    {
        if (string.IsNullOrWhiteSpace(nombres))
            throw new DomainException("Los nombres son obligatorios.");
        if (string.IsNullOrWhiteSpace(apellidos))
            throw new DomainException("Los apellidos son obligatorios.");
        if (CalcularEdad(fechaNacimiento, hoy) < EdadMinima)
            throw new DomainException($"El cliente debe ser mayor de {EdadMinima} años (RN-01).");
        if (ingresoMensual <= 0)
            throw new DomainException("El ingreso mensual debe ser mayor a 0.");
        if (gastosMensuales < 0)
            throw new DomainException("Los gastos mensuales no pueden ser negativos.");

        Nombres = nombres.Trim();
        Apellidos = apellidos.Trim();
        FechaNacimiento = fechaNacimiento;
        Telefono = Limpiar(telefono);
        Email = Limpiar(email)?.ToLowerInvariant();
        Direccion = Limpiar(direccion);
        IngresoMensual = ingresoMensual;
        GastosMensuales = gastosMensuales;
    }

    private static string ValidarDni(string dni)
    {
        dni = dni?.Trim() ?? string.Empty;
        if (dni.Length != 8 || !dni.All(char.IsAsciiDigit))
            throw new DomainException("El DNI debe tener exactamente 8 dígitos (RN-01).");
        return dni;
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
