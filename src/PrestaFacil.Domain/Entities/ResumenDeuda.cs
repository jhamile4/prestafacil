namespace PrestaFacil.Domain.Entities;

/// <summary>Lo que el cajero necesita saber antes de cobrar.</summary>
/// <param name="SaldoVencido">Capital + interés de las cuotas vencidas.</param>
/// <param name="TotalParaPonerseAlDia">Lo que debe pagar hoy para no tener cuotas vencidas (incluye mora).</param>
/// <param name="DeudaTotal">Lo que debe pagar hoy para cancelar el préstamo completo.</param>
public record ResumenDeuda(
    DateOnly FechaCorte,
    int CuotasVencidas,
    int DiasAtrasoMaximo,
    decimal SaldoVencido,
    decimal MoraPendiente,
    decimal TotalParaPonerseAlDia,
    decimal DeudaTotal,
    Cuota? ProximaCuota);
