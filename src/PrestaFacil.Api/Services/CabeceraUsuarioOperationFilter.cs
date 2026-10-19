using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PrestaFacil.Api.Services;

/// <summary>
/// TEMPORAL: agrega el campo X-Usuario-Id en Swagger para probar con distintos
/// usuarios. Se elimina cuando llegue la autenticación JWT.
/// </summary>
public class CabeceraUsuarioOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = ConfigCurrentUser.CabeceraUsuario,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Solo desarrollo. 1 = admin, 2 = asesor1, 3 = asesor2, 4 = cajero1.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer }
        });
    }
}
