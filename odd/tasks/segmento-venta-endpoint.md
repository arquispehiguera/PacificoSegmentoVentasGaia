# Feature: segmento-venta-endpoint

## Objective
Expose `GET /api/segmento` that returns the sales segment records for a Peruvian mobile number, calling the existing `SppGss_App_PacificoGaiaSegmentoVenta` stored procedure.

## Scope / Decisions
- Query parameter: `celular` (Peruvian mobile, `^9\d{8}$`), validated with FluentValidation.
- Response: always an array inside `ApiResponse<T>` (1 match -> 1 element, many -> many). No match -> 404 via `NotFoundException`.
- Campaigns (`OutboundProcessIds`) come from `appsettings.json` section `SegmentoVenta:Campanias`; joined with `,` before calling the SP (assumption: SP splits by comma — confirm with DB owner).
- Existing `ISegmentoRepository` / `SegmentoRepository` reused unchanged.

## Constraints
- Clean Architecture layering (Api / Application / Domain / Infraestructure).
- TDD: off (no test project exists; source: repository state). Checks: `dotnet build`.
- Not a git repository: work-unit commits are not possible.

## Tasks
- [x] T1 — Options + config: `SegmentoVentaConfig`, appsettings section, DI registration. Route: delegated (writer trigger, 2+ files).
- [x] T2 — Application: query DTO, validator, response DTO + mapping, `ISegmentoService` / `SegmentoService`. Route: delegated.
- [x] T3 — Api: `SegmentoController` with `GetSegmentoVentaAsync`. Route: delegated.
- [x] T4 — Verify: `dotnet build` passes. Route: inline (writer stopped on a network error before building).

## Progress / Evidence
- Writer created all files, then terminated (API network error) before building.
- First build failed on a pre-existing bug: `Program.cs` imported `...Infraestructure` while `AddInfrastructure` lives in `...Infrastructure`. Fixed the using (inline, 1 line).
- `dotnet build PacificoSegmentoVentasGaia.sln`: 0 errors, 2 pre-existing warnings (CS0105 duplicate usings in Program.cs).
- Middleware mapping: `NotFoundException` -> 404, `BusinessException` (no campaigns configured) -> 500.
- Not run: runtime/manual call against the SP (DB unreachable from this environment). No tests exist.
- No commits: folder is not a git repository.

## Next step
Manual test: `GET /api/segmento?celular=9XXXXXXXX` with a Keycloak token; confirm SP splits `OutboundProcessIds` by comma.
