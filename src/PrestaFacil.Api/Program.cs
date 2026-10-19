using System.Reflection;
using System.Text.Json.Serialization;
using PrestaFacil.Api.Middleware;
using PrestaFacil.Api.Services;
using PrestaFacil.Application;
using PrestaFacil.Application.Common;
using PrestaFacil.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ConfigCurrentUser>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    // Los estados viajan como texto ("Pendiente"), no como número
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen(options =>
{
    if (builder.Environment.IsDevelopment())
        options.OperationFilter<CabeceraUsuarioOperationFilter>();

    options.SwaggerDoc("v1", new()
    {
        Title = "PrestaFácil API",
        Version = "v1",
        Description = "Gestión de préstamos personales: clientes, evaluación, cronogramas, pagos y mora."
    });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xml);
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "PrestaFácil API");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
