using PrestaFacil.Application.Reportes;

namespace PrestaFacil.Api.Services;

/// <summary>
/// RF-14: ejecuta el proceso de mora al iniciar la API y luego cada
/// Mora:IntervaloHoras horas. El procedimiento es idempotente, así que
/// correrlo varias veces el mismo día no duplica la mora.
/// </summary>
public class MoraDiariaJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MoraDiariaJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Mora:ProcesoAutomatico", true))
        {
            logger.LogInformation("Proceso automático de mora desactivado.");
            return;
        }

        var intervalo = TimeSpan.FromHours(configuration.GetValue("Mora:IntervaloHoras", 24));
        using var timer = new PeriodicTimer(intervalo);

        do
        {
            try
            {
                // Los servicios con DbContext son Scoped: se crea un scope por ejecución
                await using var scope = scopeFactory.CreateAsyncScope();
                var reportes = scope.ServiceProvider.GetRequiredService<IReporteService>();
                var resultado = await reportes.ProcesarMoraAsync(stoppingToken);
                logger.LogInformation("Proceso de mora {Fecha}: {Cuotas} cuotas actualizadas.",
                    resultado.FechaCorte, resultado.CuotasActualizadas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Un fallo (BD caída) no debe tumbar la API; se reintenta en el siguiente ciclo
                logger.LogError(ex, "Falló el proceso de mora.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
