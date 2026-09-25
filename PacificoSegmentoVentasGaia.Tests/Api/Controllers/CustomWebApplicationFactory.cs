using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using PacificoSegmentoVentasGaia.Application.Interfaces;

namespace PacificoSegmentoVentasGaia.Tests.Api.Controllers;

/// <summary>
/// Test host for <see cref="Program"/>. Isolates the application from real infrastructure by:
/// <list type="bullet">
/// <item>Clearing every configuration source added by <c>WebApplicationBuilder.CreateBuilder</c>
/// (the real <c>appsettings.json</c>, including the Serilog MSSqlServer sink and the production
/// SQL Server connection string) and replacing it with an in-memory, dummy configuration.</item>
/// <item>Never defining a "Serilog:WriteTo" section, so <c>ReadFrom.Configuration</c> configures
/// a logger with no sinks — no MSSqlServer sink is ever constructed and no network call happens.</item>
/// <item>Pointing "ConnectionStrings:AppConnection" at a non-routable local value.</item>
/// <item>Pointing the Keycloak settings at dummy values; they are never dereferenced because the
/// JwtBearer ("Bearer") scheme is replaced as the default authenticate/challenge scheme below,
/// so its handler — and therefore the Keycloak metadata fetch — never runs.</item>
/// <item>Replacing <see cref="ISegmentoService"/> with an NSubstitute fake exposed via
/// <see cref="SegmentoServiceFake"/>.</item>
/// <item>Replacing authentication with <see cref="TestAuthHandler"/> as the default
/// authenticate AND challenge scheme.</item>
/// </list>
/// See <see cref="SegmentoControllerTests.GetSegmento_Configuration_IsIsolatedFromRealInfrastructure"/>
/// for the assertion that proves the isolation.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public ISegmentoService SegmentoServiceFake { get; } = Substitute.For<ISegmentoService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            // Drop every real configuration source (appsettings.json, appsettings.{Env}.json,
            // environment variables, etc.) so nothing production-configured — the real DB, the
            // real MSSqlServer log sink, the real Keycloak tenant — can leak into the test host.
            configBuilder.Sources.Clear();

            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppConnection"] =
                    "Server=127.0.0.1,1;Database=dummy;User Id=dummy;Password=dummy;TrustServerCertificate=True;Connect Timeout=1;",
                ["SegmentoVenta:Campanias:0"] = "TEST_CAMPANIA",
                ["Keycloak:Authority"] = "https://localhost/unused-authority",
                ["Keycloak:Audience"] = "test-audience",
                ["Keycloak:CertsUrl"] = "https://localhost/unused-certs",
                ["Logging:LogLevel:Default"] = "Warning"
                // Intentionally no "Serilog" section: ReadFrom.Configuration then configures a
                // logger with no sinks, so the MSSqlServer sink is never created.
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISegmentoService>();
            services.AddSingleton(SegmentoServiceFake);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}
