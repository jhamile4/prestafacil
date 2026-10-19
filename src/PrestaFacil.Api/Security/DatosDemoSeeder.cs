using Microsoft.EntityFrameworkCore;
using PrestaFacil.Application.Usuarios;
using PrestaFacil.Infrastructure.Persistence;

namespace PrestaFacil.Api.Security;

/// <summary>
/// SOLO DESARROLLO. database/03_seed.sql crea los usuarios con PasswordHash = 'PENDIENTE_HASH'
/// porque SQL Server no calcula BCrypt. Al iniciar, se reemplaza por el hash de
/// Demo:PasswordInicial (appsettings.Development.json).
/// </summary>
public static class DatosDemoSeeder
{
    public const string HashPendiente = "PENDIENTE_HASH";

    public static async Task AsignarPasswordsDemoAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var password = configuration["Demo:PasswordInicial"];
        if (string.IsNullOrWhiteSpace(password)) return;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrestaFacilDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var pendientes = await db.Usuarios.Where(u => u.PasswordHash == HashPendiente).ToListAsync();
        if (pendientes.Count == 0) return;

        foreach (var usuario in pendientes)
            usuario.CambiarPasswordHash(hasher.Hash(password));
        await db.SaveChangesAsync();

        logger.LogInformation("Se asignó la contraseña de demostración a {Cantidad} usuario(s).", pendientes.Count);
    }
}
