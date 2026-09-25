# Feature: log-enrichment-middleware

## Objective
Fill the `Celular` and `ContactId` additional columns of the Serilog MSSqlServer sink (`GSS_LogPacifico`) for every log written during a request, including logs written by `ExceptionHandlingMiddleware`.

## Problem / Why
The sink only fills an additional column when the log event has a property with that exact name. No code pushes `Celular`/`ContactId` into `LogContext`, so error logs (the most important ones) have NULL in both columns. Pushing the properties inside the controller does not work: the `using` scope is disposed before the exception middleware logs.

## Scope / Decisions
- New `LogEnrichmentMiddleware` in `PacificoSegmentoVentasGaia.Api/Middleware`, registered BEFORE `ExceptionHandlingMiddleware` so its `LogContext` scope wraps the error logging.
- `Celular`: read from query string `celular` (GET `api/segmento`).
- `ContactId`: read from the JSON body property `contactId` (case-insensitive) on POST `api/segmento/resultado`, using `Request.EnableBuffering()` and rewinding the stream so model binding still works. Invalid/empty/non-JSON bodies are ignored silently (never break the request).
- Properties pushed only when a value exists.
- Out of scope: moving the plain-text connection string to secrets (flagged to the user separately).

## Constraints
- TDD: off (source: existing feature docs). Runner: `dotnet test`.
- Tests never touch real infrastructure.
- Not a git repository: no commits.

## Tasks
- [x] T1 — Middleware + registration order in `Program.cs`. Route: delegated (writer trigger, 2+ files incl. tests).
- [x] T2 — Tests: Celular from query, ContactId from body, body still readable downstream, invalid JSON ignored, property present in a log written by a downstream component that throws. Route: delegated (same writer).
- [x] T3 — Verify `dotnet build` + `dotnet test`. Route: writer, parent spot check.

## Acceptance criteria
- A log written anywhere during a GET with `celular` carries `Celular`.
- A log written anywhere during the POST carries `ContactId`, including the exception middleware's error log.
- The POST controller still receives the full body.

## Progress / Evidence
- Created `PacificoSegmentoVentasGaia.Api/Middleware/LogEnrichmentMiddleware.cs`: pushes `Celular`
  from query string `celular` (if non-empty) and `ContactId` from the root JSON body property
  `contactId` (case-insensitive, only when its value is a non-empty string) into `LogContext`,
  wrapping `await _next(context)` so both stay in scope for every downstream log (including
  `ExceptionHandlingMiddleware`'s error log). Uses `Request.EnableBuffering()` + rewinds
  `Request.Body.Position = 0` in a `finally`, skips parsing bodies without a JSON content type,
  without a body, or over 1 MB (`ContentLength`), swallows `JsonException`, and respects
  `context.RequestAborted`.
- `Program.cs`: registered `app.UseMiddleware<LogEnrichmentMiddleware>();` immediately before
  `app.UseMiddleware<ExceptionHandlingMiddleware>();`. No other line changed.
  `Enrich.FromLogContext()` was already present (code + `appsettings.json`); left as is.
- Added `PacificoSegmentoVentasGaia.Tests/Api/Middleware/LogEnrichmentMiddlewareTests.cs`: 6 xUnit
  tests against a real `Serilog.Core.Logger` (`Enrich.FromLogContext()` + an in-memory
  `ILogEventSink`), no new NuGet package needed (Serilog comes transitively via the Api project
  reference). Cases: Celular from query; ContactId from body + body still fully readable
  downstream; case-insensitive `ContactId`/`contactId`; invalid JSON body → no exception, no
  ContactId, body still readable; no celular/no body → next still called, no properties pushed;
  and the core-bug regression case — `LogEnrichmentMiddleware` → fake exception-handler delegate
  (catches and logs) → throwing delegate — asserting the error-level log event still carries
  `ContactId`.

## Verification
- `dotnet build PacificoSegmentoVentasGaia.sln`: succeeded, 0 warnings, 0 errors.
- `dotnet test PacificoSegmentoVentasGaia.sln`: 67/67 passed, 0 failed, 0 skipped (61 pre-existing + 6 new). All existing tests stayed green.

## Status: done. No open concerns.
