# Feature: segmento-tests

## Objective
Add a test project covering the Segmento Venta use case so regressions are caught with `dotnet test`.

## Scope
- New project `PacificoSegmentoVentasGaia.Tests` (xUnit, NSubstitute, Microsoft.AspNetCore.Mvc.Testing, FluentValidation.TestHelper), added to the solution.
- Unit: `SegmentoVentaQueryValidator`, `SegmentoService`, AutoMapper configuration.
- Integration: `GET api/segmento` through `WebApplicationFactory` with fake auth and a substituted service (401 / 400 / 200 / 404 / 500).
- Minimal production change allowed: expose `Program` to the test host (`public partial class Program;`).
- Out of scope: repository/SP tests against a real database.

## Constraints
- Tests must never touch real infrastructure: no SQL Server (including the Serilog MSSqlServer sink), no Keycloak.
- Plain xUnit `Assert` (no FluentAssertions).
- TDD: off (characterization tests for existing code). Runner: `dotnet test`.
- Not a git repository: no commits.

## Tasks
- [x] T1 — Create test project + solution entry. Route: delegated (writer trigger).
- [x] T2 — Validator + service + mapping unit tests. Route: delegated.
- [x] T3 — Endpoint integration tests with isolated config. Route: delegated.
- [x] T4 — Verify: `dotnet build` + `dotnet test` green.

## Progress / Evidence

### Files created
- `PacificoSegmentoVentasGaia.Tests/PacificoSegmentoVentasGaia.Tests.csproj` (xunit template, net9.0; added to `PacificoSegmentoVentasGaia.sln`; project refs: Api, Application, Domain; packages: NSubstitute 6.2.0, Microsoft.AspNetCore.Mvc.Testing 9.0.5, FluentValidation 11.11.0 — no FluentAssertions, no Moq).
- `PacificoSegmentoVentasGaia.Tests/Application/Validators/SegmentoVentaQueryValidatorTests.cs` — 10 tests (1 valid `[Fact]` + 9 invalid cases via `[Theory]`/`[InlineData]`, `TestValidate`/`ShouldHaveValidationErrorFor`).
- `PacificoSegmentoVentasGaia.Tests/Application/Mappings/SegmentoMappingProfileTests.cs` — 1 test (`AssertConfigurationIsValid`).
- `PacificoSegmentoVentasGaia.Tests/Application/Services/SegmentoServiceTests.cs` — 8 tests: campaign join/trim/skip-blank (asserts exact string via `Received(1)`), celular passthrough, empty/all-blank campaigns → `BusinessException` + repo never called, empty repo result → `NotFoundException` whose message excludes the phone number, one row → mapped fields (`ContactId`, `DNI`), several rows → same count, `CancellationToken` forwarded. Uses NSubstitute for `ISegmentoRepository` and a real `MapperConfiguration` built from `SegmentoMappingProfile`.
- `PacificoSegmentoVentasGaia.Tests/Api/Controllers/TestAuthHandler.cs` — auth scheme that authenticates only when header `Authorization: Test` is present; otherwise `AuthenticateResult.NoResult()`.
- `PacificoSegmentoVentasGaia.Tests/Api/Controllers/CustomWebApplicationFactory.cs` — `WebApplicationFactory<Program>`; replaces `ISegmentoService` with an NSubstitute fake (`SegmentoServiceFake`); sets `TestAuthHandler` as default authenticate + challenge scheme; clears all config sources and re-adds an in-memory dummy config (no `Serilog` section, dummy `ConnectionStrings:AppConnection`, dummy `Keycloak:*`).
- `PacificoSegmentoVentasGaia.Tests/Api/Controllers/SegmentoControllerTests.cs` — 8 tests: no auth → 401; auth + `celular=123` → 400 (`ApiValidationErrorResponse`, `success=false`, non-empty `errors`); auth + missing celular → 400; auth + valid celular + service returns 2 items → 200 (`ApiResponse`, `success=true`, `data.Count == 2`); service throws `NotFoundException` → 404 `success=false`; service throws `BusinessException` → 500 generic message (asserted not to contain the real exception detail); service throws `InvalidOperationException` → 500; plus one isolation-proof test (see below).
- `PacificoSegmentoVentasGaia.Tests/TestEnvironmentBootstrap.cs` — `[ModuleInitializer]` that sets process env vars before any test/host code runs (see isolation section below).

### Production change
- `PacificoSegmentoVentasGaia.Api/Program.cs` — added `public partial class Program;` after the top-level statements, so `WebApplicationFactory<Program>` can host the app. No other production code touched.

### Build + test output
- `dotnet build PacificoSegmentoVentasGaia.sln` → **0 errors**, 3 warnings (all `CA1416`: "`Program` is only supported on 'windows'" — the Api assembly carries `[assembly: SupportedOSPlatform("windows")]`, and the Tests project doesn't declare the same platform, so referencing `Program`/`WebApplicationFactory<Program>` from 3 call sites in `SegmentoControllerTests.cs` triggers the analyzer. Harmless on this Windows box; not fixed since it would mean touching non-test config beyond what's authorized).
- `dotnet test PacificoSegmentoVentasGaia.sln` → **27/27 passed, 0 failed, 0 skipped**, ~3.4s total.
  - Validator: 10, Mapping: 1, Service: 8, Controller (integration): 8.

### Infra isolation — how it was achieved and proven
Two layers were required, discovered empirically:
1. **`ConfigureAppConfiguration` in `CustomWebApplicationFactory`** (`Sources.Clear()` + a curated `AddInMemoryCollection`) — this only takes effect at `WebApplicationBuilder.Build()` time (it's wired through `WebApplicationFactory`'s deferred `IHostingStartup`-based hook). It correctly isolates everything resolved at/after `Build()`: `DapperContext` (a singleton; `ValidateOnBuild`, on by default outside Production, eagerly constructs every singleton at `Build()` and would otherwise throw on a missing connection string), `Keycloak:*`, `SegmentoVenta:Campanias`.
2. **`TestEnvironmentBootstrap` (`[ModuleInitializer]`, sets process env vars)** — required because `Program.cs` builds `Log.Logger` from `builder.Configuration` immediately after `CreateBuilder(args)` and *before* `Build()` runs, i.e. before layer 1 above ever applies. **This was verified empirically**: with only layer 1 in place, `dotnet test` took ~41s and the stack trace showed `Serilog.Sinks.MSSqlServer.MSSqlServerSink.CreateDatabaseAndTable` executing against the real config (real `10.151.63.162` server, from `appsettings.json`) — i.e. tests were about to reach real infrastructure. The fix: env vars (`Serilog__WriteTo__0__Args__connectionString`, `Serilog__WriteTo__0__Args__autoCreateSqlTable=false`, `ConnectionStrings__AppConnection`, `Keycloak__*`) are read by `AddEnvironmentVariables()`, one of the default sources `CreateBuilder(args)` adds *synchronously*, so they're visible before the logger is built. `autoCreateSqlTable=false` skips the sink's eager DB-touching constructor step entirely; the dummy `127.0.0.1,1` connection string makes any later background batch-flush (period 10s) fail fast locally instead of reaching the real server. After this fix, the full suite (including the integration tests) ran in ~3.4s with no hang.
- **Proof test**: `SegmentoControllerTests.GetSegmento_Configuration_IsIsolatedFromRealInfrastructure` resolves the running host's `IConfiguration` and asserts `Serilog:WriteTo:0:Name` is null (no sink section survives `Sources.Clear()`), the resolved `ConnectionStrings:AppConnection` contains `127.0.0.1` and not `10.151.63.162`, and `Keycloak:Authority` is not the real tenant URL. This test is green.
- JSON responses confirmed camelCase (default ASP.NET Core behavior, unchanged); no `JsonSerializerOptions` override needed since `System.Text.Json`'s default web options already apply via MVC's default output formatter in this project.

### Findings / bugs
None found. All characterization tests passed against the existing production code as-is; no test was skipped or weakened.

## Next step
None — feature complete. All 4 tasks done and verified.
