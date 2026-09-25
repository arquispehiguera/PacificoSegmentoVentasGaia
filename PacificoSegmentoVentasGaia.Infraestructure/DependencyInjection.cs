using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PacificoSegmentoVentasGaia.Application.Options;
using PacificoSegmentoVentasGaia.Domain.Interfaces;
using PacificoSegmentoVentasGaia.Infraestructure.Repositories;
using PacificoSegmentoVentasGaia.Infraestructure.Data;
using Polly;
using Polly.Retry;

namespace PacificoSegmentoVentasGaia.Infraestructure;

public static class DependencyInjection
{
    // Códigos de error transitorios de SQL Server
    private static readonly int[] TransientSqlErrors =
    [
        49920, 49919, 49918, 40613, 40501, 40197,
        10929, 10928, 4060, 233, 64, 20,
        1205  // deadlock
    ];

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DapperContext>();
        services.AddScoped<ISegmentoRepository, SegmentoRepository>();

        services.Configure<SegmentoVentaConfig>(configuration.GetSection("SegmentoVenta"));

        // Pipeline DB — retry solo en errores transitorios de SQL Server
        var dbPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay            = TimeSpan.FromSeconds(1),
                BackoffType      = DelayBackoffType.Exponential,
                ShouldHandle     = new PredicateBuilder()
                    .Handle<SqlException>(ex => TransientSqlErrors.Contains(ex.Number))
                    .Handle<TimeoutException>()
            })
            .Build();

        services.AddKeyedSingleton<ResiliencePipeline>("db", dbPipeline);

        return services;
    }
}
