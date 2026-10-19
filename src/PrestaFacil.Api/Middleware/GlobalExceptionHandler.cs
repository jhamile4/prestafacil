using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestaFacil.Application.Common;
using PrestaFacil.Domain.Common;

namespace PrestaFacil.Api.Middleware;

/// <summary>
/// Convierte las excepciones en respuestas ProblemDetails (RFC 9457),
/// así los controladores no necesitan try/catch.
/// </summary>
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException ve => new ValidationProblemDetails(
                ve.Errors
                  .GroupBy(e => ToCamelCase(e.PropertyName))
                  .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Hay datos inválidos en la petición."
            },
            UnauthorizedException => Crear(StatusCodes.Status401Unauthorized, "No autorizado", exception.Message),
            NotFoundException => Crear(StatusCodes.Status404NotFound, "Recurso no encontrado", exception.Message),
            ConflictException => Crear(StatusCodes.Status409Conflict, "Conflicto con datos existentes", exception.Message),
            DbUpdateConcurrencyException => Crear(StatusCodes.Status409Conflict, "Conflicto de concurrencia",
                "Otro usuario modificó este préstamo al mismo tiempo. Vuelva a consultar la deuda e intente de nuevo."),
            DomainException => Crear(StatusCodes.Status422UnprocessableEntity, "Regla de negocio no cumplida", exception.Message),
            _ => Crear(StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado en {Path}", context.Request.Path);

        context.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Crear(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };

    private static string ToCamelCase(string nombre) =>
        string.IsNullOrEmpty(nombre) ? nombre : char.ToLowerInvariant(nombre[0]) + nombre[1..];
}
