using System.Runtime.CompilerServices;

namespace PacificoSegmentoVentasGaia.Tests;

/// <summary>
/// Sets process environment variables before any test runs, and therefore before
/// <c>PacificoSegmentoVentasGaia.Api.Program</c> ever executes.
///
/// <para>
/// This exists because Program.cs builds the Serilog logger directly from
/// <c>builder.Configuration</c> (<c>Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration)...CreateLogger()</c>)
/// immediately after <c>WebApplication.CreateBuilder(args)</c> and BEFORE
/// <c>WebApplicationBuilder.Build()</c> is ever called. <see cref="Api.Controllers.CustomWebApplicationFactory"/>'s
/// <c>ConfigureAppConfiguration</c> hook only takes effect when <c>Build()</c> runs, which is too
/// late to influence that line: an earlier attempt confirmed this empirically — with only the
/// <c>ConfigureAppConfiguration</c> override in place, the MSSqlServer sink was still constructed
/// against the real appsettings.json connection string and tried to dial the real
/// 10.151.63.162 server (observed as a ~40s hang in <c>MSSqlServerSink.CreateDatabaseAndTable</c>
/// before this bootstrap was added).
/// </para>
///
/// <para>
/// Environment variables, in contrast, are read by <c>AddEnvironmentVariables()</c>, one of the
/// default configuration sources <c>CreateBuilder(args)</c> adds synchronously to
/// <c>builder.Configuration</c> — so they ARE visible at <c>CreateBuilder</c> time, before the
/// logger is built. Setting them here, in a <see cref="ModuleInitializerAttribute"/> method, runs
/// once when this test assembly loads (before any test executes), guaranteeing they are in place
/// before the first <c>CustomWebApplicationFactory</c> boots the app.
/// </para>
/// </summary>
internal static class TestEnvironmentBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Override only the two leaf Args keys the real appsettings.json's MSSqlServer sink
        // reads (connectionString, autoCreateSqlTable) rather than swapping "Name" to a
        // different sink: swapping would leave the base config's other "Args:*" leftover keys
        // (e.g. tableName, columnOptionsSection) around with no matching parameter on the new
        // sink's method, which Serilog.Settings.Configuration would fail to bind.
        // - autoCreateSqlTable=false skips the eager CreateDatabaseAndTable call the sink's
        //   constructor otherwise makes synchronously (this is what hung for ~40s against the
        //   real server before this fix).
        // - connectionString points at a non-routable local value so that any background batch
        //   flush the sink attempts later (period: 10s) fails fast locally instead of reaching
        //   the real server.
        Environment.SetEnvironmentVariable(
            "Serilog__WriteTo__0__Args__connectionString",
            "Data Source=127.0.0.1,1;Initial Catalog=dummy;Connect Timeout=1;TrustServerCertificate=True;");
        Environment.SetEnvironmentVariable("Serilog__WriteTo__0__Args__autoCreateSqlTable", "false");

        // Same reasoning for the app's own DB connection string: DapperContext is a singleton,
        // and ASP.NET Core's ValidateOnBuild (enabled by default outside "Production") eagerly
        // constructs every singleton at WebApplicationBuilder.Build() time, so this must be a
        // valid, non-null, non-routable value before Build() runs.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__AppConnection",
            "Server=127.0.0.1,1;Database=dummy;User Id=dummy;Password=dummy;TrustServerCertificate=True;Connect Timeout=1;");

        // Keycloak metadata must never be fetched. These are never actually dereferenced in
        // tests (the JwtBearer "Bearer" scheme is replaced as the default authenticate/challenge
        // scheme by CustomWebApplicationFactory, so its handler — and this config — is never
        // touched), but are set to obviously-unusable values as defense in depth.
        Environment.SetEnvironmentVariable("Keycloak__Authority", "https://localhost/unused-authority");
        Environment.SetEnvironmentVariable("Keycloak__Audience", "test-audience");
        Environment.SetEnvironmentVariable("Keycloak__CertsUrl", "https://localhost/unused-certs");
    }
}
