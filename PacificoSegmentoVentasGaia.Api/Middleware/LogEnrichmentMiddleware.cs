using System.Text.Json;
using Serilog.Context;

namespace PacificoSegmentoVentasGaia.Api.Middleware;

/// <summary>
/// Enriquece el contexto de logging (<see cref="LogContext"/>) con datos de negocio extraídos de
/// la petición actual, para que las columnas adicionales <c>Celular</c> y <c>ContactId</c> del
/// sink de Serilog (MSSqlServer) se completen en TODOS los eventos escritos durante la petición,
/// incluyendo los que registra <see cref="ExceptionHandlingMiddleware"/>.
/// </summary>
/// <remarks>
/// Debe registrarse ANTES que <see cref="ExceptionHandlingMiddleware"/> en el pipeline
/// (<c>Program.cs</c>). El scope de <see cref="LogContext.PushProperty"/> solo enriquece los logs
/// escritos mientras el <c>using</c> que lo sostiene sigue abierto; como este middleware envuelve
/// la llamada a <c>_next</c> (que incluye a <see cref="ExceptionHandlingMiddleware"/> y al resto
/// del pipeline), cualquier log emitido más adelante —incluido el log de error del middleware de
/// excepciones— queda dentro del scope y hereda las propiedades. Empujar estas propiedades dentro
/// del controlador, en cambio, no sirve: su scope se cierra antes de que el middleware de
/// excepciones llegue a loguear.
/// </remarks>
public class LogEnrichmentMiddleware
{
    private const string CelularQueryParam = "celular";
    private const string ContactIdJsonProperty = "contactId";
    private const long MaxBodyBytesToInspect = 1 * 1024 * 1024; // 1 MB

    private readonly RequestDelegate _next;
    private readonly ILogger<LogEnrichmentMiddleware> _logger;

    public LogEnrichmentMiddleware(RequestDelegate next, ILogger<LogEnrichmentMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var celular = ExtraerCelularDeQuery(context);
        var contactId = await ExtraerContactIdDeBodyAsync(context);

        using (PushPropertyIfPresent("Celular", celular))
        using (PushPropertyIfPresent("ContactId", contactId))
        {
            await _next(context);
        }
    }

    private static string? ExtraerCelularDeQuery(HttpContext context)
    {
        var celular = context.Request.Query[CelularQueryParam].ToString();
        return string.IsNullOrEmpty(celular) ? null : celular;
    }

    /// <summary>
    /// Busca la propiedad <c>contactId</c> (comparación case-insensitive) en la raíz del cuerpo
    /// JSON de la petición, sin romper el binding posterior del modelo: habilita el buffering del
    /// body y rebobina el stream a la posición 0 en el <c>finally</c>, para que el resto del
    /// pipeline (model binding, validación) siga pudiendo leerlo íntegro.
    /// </summary>
    private async Task<string?> ExtraerContactIdDeBodyAsync(HttpContext context)
    {
        var request = context.Request;

        if (!request.HasJsonContentType() || request.ContentLength is not > 0)
        {
            return null;
        }

        if (request.ContentLength > MaxBodyBytesToInspect)
        {
            return null;
        }

        request.EnableBuffering();

        try
        {
            using var document = await JsonDocument.ParseAsync(
                request.Body,
                cancellationToken: context.RequestAborted);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, ContactIdJsonProperty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                var contactId = property.Value.GetString();
                return string.IsNullOrEmpty(contactId) ? null : contactId;
            }

            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "No se pudo interpretar el cuerpo de la petición como JSON para extraer ContactId.");
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static IDisposable PushPropertyIfPresent(string propertyName, string? value)
        => value is null ? NoopDisposable.Instance : LogContext.PushProperty(propertyName, value);

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }
}
