namespace PrestaFacil.Domain.Services;

/// <summary>Datos del cliente que necesita la evaluación, ya consultados de la BD.</summary>
public record SituacionCliente(
    bool ClienteActivo,
    decimal IngresoNeto,
    int PrestamosActivos,
    decimal CuotasMensualesActuales,
    bool TieneCuotasVencidas);

public record ReglaEvaluada(string Codigo, string Descripcion, bool Cumple, string Detalle);

public record ResultadoEvaluacion(
    decimal CuotaNueva,
    decimal IngresoNeto,
    decimal LimiteCuotas,
    IReadOnlyList<ReglaEvaluada> Reglas)
{
    public bool Apto => Reglas.All(r => r.Cumple);

    public IEnumerable<ReglaEvaluada> Incumplidas => Reglas.Where(r => !r.Cumple);
}

/// <summary>
/// Reglas RN-04 a RN-07. Siempre evalúa todas para mostrarle al asesor
/// el motivo completo, no solo la primera regla que falla.
/// </summary>
public static class EvaluadorCredito
{
    /// <summary>RN-04: las cuotas no pueden superar este porcentaje del ingreso neto.</summary>
    public const decimal PorcentajeMaximoIngreso = 0.40m;

    /// <summary>RN-05</summary>
    public const int MaximoPrestamosActivos = 2;

    public static ResultadoEvaluacion Evaluar(SituacionCliente cliente, decimal cuotaNueva)
    {
        var limite = CalculadoraPrestamo.Redondear(Math.Max(0, cliente.IngresoNeto) * PorcentajeMaximoIngreso);
        var totalCuotas = cliente.CuotasMensualesActuales + cuotaNueva;

        var reglas = new List<ReglaEvaluada>
        {
            new("RN-07", "Cliente activo",
                cliente.ClienteActivo,
                cliente.ClienteActivo ? "El cliente está activo." : "El cliente está desactivado."),

            new("RN-04", "Capacidad de pago (máx. 40 % del ingreso neto)",
                totalCuotas <= limite,
                $"Cuotas mensuales: S/ {totalCuotas:N2} (actuales S/ {cliente.CuotasMensualesActuales:N2} + nueva S/ {cuotaNueva:N2}). " +
                $"Límite: S/ {limite:N2} (40 % de S/ {cliente.IngresoNeto:N2})."),

            new("RN-05", $"Máximo {MaximoPrestamosActivos} préstamos activos",
                cliente.PrestamosActivos < MaximoPrestamosActivos,
                $"Tiene {cliente.PrestamosActivos} préstamo(s) activo(s) o aprobado(s)."),

            new("RN-06", "Sin cuotas vencidas",
                !cliente.TieneCuotasVencidas,
                cliente.TieneCuotasVencidas ? "Tiene cuotas vencidas en otro préstamo." : "No tiene cuotas vencidas.")
        };

        return new ResultadoEvaluacion(cuotaNueva, cliente.IngresoNeto, limite, reglas);
    }
}
