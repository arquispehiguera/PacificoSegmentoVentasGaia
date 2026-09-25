using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PacificoSegmentoVentasGaia.Api.Middleware;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PacificoSegmentoVentasGaia.Tests.Api.Middleware;

/// <summary>
/// Covers <see cref="LogEnrichmentMiddleware"/> directly against a real Serilog
/// <see cref="Logger"/> built with <c>Enrich.FromLogContext()</c> and an in-memory sink, so the
/// assertions exercise the actual <c>LogContext</c> enrichment mechanism the production pipeline
/// relies on rather than a mock of it.
/// </summary>
public class LogEnrichmentMiddlewareTests
{
    /// <summary>Minimal in-memory <see cref="ILogEventSink"/> that just collects emitted events.</summary>
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static (Logger Logger, CollectingSink Sink) CreateLogger()
    {
        var sink = new CollectingSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Sink(sink)
            .CreateLogger();
        return (logger, sink);
    }

    private static LogEnrichmentMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<LogEnrichmentMiddleware>.Instance);

    private static DefaultHttpContext CreateContext(string? queryString = null)
    {
        var context = new DefaultHttpContext();
        if (queryString is not null)
        {
            context.Request.QueryString = new QueryString(queryString);
        }
        return context;
    }

    private static void SetJsonBody(HttpContext context, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = bytes.Length;
    }

    private static string GetPropertyValue(LogEvent logEvent, string propertyName)
    {
        var scalar = Assert.IsType<ScalarValue>(logEvent.Properties[propertyName]);
        return Assert.IsType<string>(scalar.Value);
    }

    [Fact]
    public async Task InvokeAsync_GetWithCelularQuery_LogsCelularProperty()
    {
        var (logger, sink) = CreateLogger();
        var context = CreateContext("?celular=987654321");

        var middleware = CreateMiddleware(_ =>
        {
            logger.Information("evento de prueba");
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal("987654321", GetPropertyValue(logEvent, "Celular"));
    }

    [Fact]
    public async Task InvokeAsync_PostWithContactIdInBody_LogsContactIdAndBodyStillReadableDownstream()
    {
        var (logger, sink) = CreateLogger();
        var context = CreateContext();
        const string originalJson = """{"contactId":"C-1","otro":"valor"}""";
        SetJsonBody(context, originalJson);

        string? bodyReadDownstream = null;
        var middleware = CreateMiddleware(async ctx =>
        {
            using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
            bodyReadDownstream = await reader.ReadToEndAsync();
            logger.Information("evento de prueba");
        });

        await middleware.InvokeAsync(context);

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal("C-1", GetPropertyValue(logEvent, "ContactId"));
        Assert.Equal(originalJson, bodyReadDownstream);
    }

    [Fact]
    public async Task InvokeAsync_ContactIdPropertyWithDifferentCasing_IsFoundCaseInsensitively()
    {
        var (logger, sink) = CreateLogger();
        var context = CreateContext();
        SetJsonBody(context, """{"ContactId":"C-2"}""");

        var middleware = CreateMiddleware(_ =>
        {
            logger.Information("evento de prueba");
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal("C-2", GetPropertyValue(logEvent, "ContactId"));
    }

    [Fact]
    public async Task InvokeAsync_InvalidJsonBody_DoesNotThrowAndBodyStillReadableDownstream()
    {
        var (logger, sink) = CreateLogger();
        var context = CreateContext();
        const string invalidJson = "{not valid json";
        SetJsonBody(context, invalidJson);

        string? bodyReadDownstream = null;
        var middleware = CreateMiddleware(async ctx =>
        {
            using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
            bodyReadDownstream = await reader.ReadToEndAsync();
            logger.Information("evento de prueba");
        });

        var exception = await Record.ExceptionAsync(() => middleware.InvokeAsync(context));

        Assert.Null(exception);
        var logEvent = Assert.Single(sink.Events);
        Assert.False(logEvent.Properties.ContainsKey("ContactId"));
        Assert.Equal(invalidJson, bodyReadDownstream);
    }

    [Fact]
    public async Task InvokeAsync_NoCelularAndNoBody_CallsNextWithoutPushingProperties()
    {
        var (logger, sink) = CreateLogger();
        var context = CreateContext();

        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            logger.Information("evento de prueba");
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        var logEvent = Assert.Single(sink.Events);
        Assert.False(logEvent.Properties.ContainsKey("Celular"));
        Assert.False(logEvent.Properties.ContainsKey("ContactId"));
    }

    [Fact]
    public async Task InvokeAsync_DownstreamExceptionHandlerLogsError_ErrorEventStillCarriesContactId()
    {
        // Reproduces the real pipeline order: LogEnrichmentMiddleware -> ExceptionHandlingMiddleware
        // (faked here) -> a component that throws. This is the core bug being fixed: the pushed
        // LogContext properties must still be active when the exception handler logs the error.
        var (logger, sink) = CreateLogger();
        var context = CreateContext();
        SetJsonBody(context, """{"contactId":"C-3"}""");

        RequestDelegate throwingDelegate = _ => throw new InvalidOperationException("boom");
        RequestDelegate fakeExceptionHandler = async ctx =>
        {
            try
            {
                await throwingDelegate(ctx);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error inesperado: {Message}", ex.Message);
            }
        };

        var middleware = CreateMiddleware(fakeExceptionHandler);

        var exception = await Record.ExceptionAsync(() => middleware.InvokeAsync(context));

        Assert.Null(exception);
        var logEvent = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Error, logEvent.Level);
        Assert.Equal("C-3", GetPropertyValue(logEvent, "ContactId"));
    }
}
