using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PrestaFacil.Api.Controllers;
using PrestaFacil.Api.Middleware;
using PrestaFacil.Api.Security;
using PrestaFacil.Api.Services;
using PrestaFacil.Application;
using PrestaFacil.Application.Common;
using PrestaFacil.Application.Usuarios;
using PrestaFacil.Infrastructure;
using PrestaFacil.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- Autenticación JWT (RF-19, RNF-01) ----------
var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>() ?? new JwtOptions();
jwt.Validar();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Conserva los nombres cortos de los claims ("sub", "role") en lugar de URIs largas
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(jwt.KeyBytes),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = JwtTokenService.ClaimRol
        };
        // Un token firmado sigue siendo válido hasta que expira. Para que desactivar
        // a un usuario o cambiarle el rol tenga efecto inmediato, se revisa en la BD
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var rol = context.Principal?.FindFirst(JwtTokenService.ClaimRol)?.Value;
                var usuarios = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
                var usuario = int.TryParse(sub, out var id) ? await usuarios.ObtenerPorIdAsync(id) : null;

                if (usuario is null || !usuario.Activo || Roles.Nombre(usuario.Rol) != rol)
                    context.Fail("El usuario fue desactivado o cambió de rol. Inicie sesión de nuevo.");
            }
        };
    });

// RF-20: todo endpoint exige sesión salvo los marcados con [AllowAnonymous];
// los roles se indican en cada controlador con [Authorize(Roles = ...)]
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Frena ataques de fuerza bruta contra el login
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(LimitesLogin.Politica, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = LimitesLogin.IntentosPorMinuto,
            Window = TimeSpan.FromMinutes(1)
        }));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddHostedService<MoraDiariaJob>();
// GET /health: la usan Docker y el CI para saber si la API y SQL Server responden
builder.Services.AddHealthChecks().AddDbContextCheck<PrestaFacilDbContext>("sqlserver");
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    // Los estados viajan como texto ("Pendiente"), no como número
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "PrestaFácil API",
        Version = "v1",
        Description = "Gestión de préstamos personales: clientes, evaluación, cronogramas, pagos y mora. " +
                      "Inicie sesión en POST /api/auth/login y pegue el token en **Authorize**."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token obtenido en POST /api/auth/login"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xml);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    await DatosDemoSeeder.AsignarPasswordsDemoAsync(app.Services, app.Configuration, app.Logger);

app.UseExceptionHandler();
// Respuestas 401, 403 y 429 sin cuerpo se completan con ProblemDetails
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "PrestaFácil API";
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
