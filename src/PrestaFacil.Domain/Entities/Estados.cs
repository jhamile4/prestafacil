namespace PrestaFacil.Domain.Entities;

/// <summary>Ciclo de vida del préstamo (ver docs/01_requerimientos.md, sección 4.2).</summary>
public enum EstadoPrestamo
{
    Pendiente,
    Aprobado,
    Rechazado,
    Desembolsado,
    Pagado
}

public enum EstadoCuota
{
    Pendiente,
    Parcial,
    Vencida,
    Pagada
}
