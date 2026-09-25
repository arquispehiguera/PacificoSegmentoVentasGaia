using PacificoSegmentoVentasGaia.Application.Common;
using PacificoSegmentoVentasGaia.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace PacificoSegmentoVentasGaia.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response;

        switch (exception)
        {
            case AuthenticationFailedException authFailed:
                _logger.LogWarning(authFailed, "Autenticación fallida: {Message}", authFailed.Message);
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = Api401Response.Fail(authFailed.Message);
                break;

            case NotFoundException notFound:
                _logger.LogWarning(notFound, "Recurso no encontrado: {Message}", notFound.Message);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = ApiErrorResponse.Fail(notFound.Message);
                break;

            case ValidationException validation:
                _logger.LogWarning(validation, "Error de validación: {Message}", validation.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var validationErrors = validation.Errors
                    .SelectMany(e => e.Value.Select(msg => $"{e.Key}: {msg}"))
                    .ToList();
                response = ApiValidationErrorResponse.Fail(validation.Message, validationErrors);
                break;

            case BusinessException business:
                _logger.LogError(business, "Error de negocio: {Message}", business.Message);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = Api500Response.Fail();
                break;

            default:
                _logger.LogError(exception, "Error inesperado: {Message}", exception.Message);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = Api500Response.Fail();
                break;
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
