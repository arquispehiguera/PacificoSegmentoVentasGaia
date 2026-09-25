# Feature: remove-leads-hangfire-inconcert

## Objective
Reduce the API to the Segmento Venta use case only: remove Hangfire, the InConcert SOAP integration, and everything related to Leads.

## Scope
- Remove Hangfire: registration, dashboard, packages.
- Remove InConcert: service, interface, options, Connected Services SOAP reference, `soap` resilience pipeline, config section, WCF packages if unused.
- Remove Leads: service, job, DTO, validator, mapping, entity, repositories (lead + contact skill), lote config and `LotePrefixes` section.
- Keep: Segmento feature, auth (Keycloak), Serilog, Swagger, middleware, `db` resilience pipeline, common responses.
- Out of scope: database objects (Hangfire tables, SPs) are not touched.

## Constraints
- TDD: off (no test project). Check: `dotnet build`.
- Not a git repository. Pre-cleanup backup: session scratchpad `backup-pre-cleanup.tar`.

## Tasks
- [x] T1 — Remove Hangfire, InConcert and Leads code/config/packages. Route: direct inline writer (single-writer subagent invocation, no further delegation).
- [x] T2 — Verify: `dotnet build` passes, no leftover references.

## Progress / Evidence
- Backup created (84 entries).
- All Hangfire/InConcert/Lead source files deleted; DI, Program.cs, csproj, and appsettings updated.
- `dotnet build PacificoSegmentoVentasGaia.sln`: Compilación correcta, 0 Advertencia(s), 0 Errores.
- Case-insensitive search for `hangfire|inconcert|lead|ServiceReference|LoteConfig` across source (excluding bin/obj/.vs/odd): 0 matches.

## Next step
None — cleanup complete.
