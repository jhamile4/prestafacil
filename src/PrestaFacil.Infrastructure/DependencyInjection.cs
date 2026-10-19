using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrestaFacil.Application.Clientes;
using PrestaFacil.Application.Prestamos;
using PrestaFacil.Application.Productos;
using PrestaFacil.Infrastructure.Persistence;
using PrestaFacil.Infrastructure.Queries;
using PrestaFacil.Infrastructure.Repositories;

namespace PrestaFacil.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PrestaFacil")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'PrestaFacil' en appsettings.");

        services.AddDbContext<PrestaFacilDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IEstadoCuentaQuery, EstadoCuentaQuery>();
        services.AddScoped<IPrestamoRepository, PrestamoRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();

        return services;
    }
}
