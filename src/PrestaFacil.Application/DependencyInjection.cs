using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PrestaFacil.Application.Clientes;
using PrestaFacil.Application.Pagos;
using PrestaFacil.Application.Prestamos;
using PrestaFacil.Application.Reportes;
using PrestaFacil.Application.Usuarios;
using PrestaFacil.Application.Productos;

namespace PrestaFacil.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IPrestamoService, PrestamoService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<IReporteService, ReporteService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        return services;
    }
}
