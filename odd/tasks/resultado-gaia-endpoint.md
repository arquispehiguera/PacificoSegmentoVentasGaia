# Feature: resultado-gaia-endpoint

## Objective
Expose `POST /api/segmento/resultado` so the GAIA bot can send the post-interaction result. The endpoint stores it in `dbo.GSS_SegmentoGaia` and marks the related management record as integrated (`GSS_Gestiones.IntegracionGaia = 1`).

## Problem / Why
GAIA already reads `GET /api/segmento` before the interaction, but there is no way to persist the interaction outcome for tracking and reporting, nor to close the management record.

## Scope / Decisions
- Request body: every `GSS_SegmentoGaia` column except `Id` and `FechaCreacion`. `ContactId` and `InteractionId` are required. Max lengths match the table columns (FluentValidation).
- Persistence: inline SQL through Dapper, using the INSERT and UPDATE supplied by the user, executed in ONE transaction on one connection.
- **[T6 update]** Multiple `GSS_SegmentoGaia` rows for the same `ContactId + InteractionId` are valid business data (the bot can report several interactions/results for the same contact/interaction pair). The duplicate check was removed entirely: no exists query, no `ConflictException`, no 409 response. The endpoint always inserts.
- **[T7 update]** The GAIA result must always be persisted, even if no `GSS_Gestiones` row matches `ContactId + LastInteractionId` (rolling back the insert would cause problems later). If the UPDATE affects 0 rows, the transaction still commits; no exception is thrown, no 404 on this path. The service logs a warning (`ContactId`/`InteractionId` as structured properties) when the gestión was not found. The response carries `GestionActualizada` (bool) alongside `Id`.
- Success: always 201 with `ApiResponse<T>` containing the new `Id` and `GestionActualizada`.
- Same auth (`[Authorize]`), response envelopes, and middleware error mapping as the existing endpoint.

## Constraints
- Clean Architecture layering (Api / Application / Domain / Infraestructure).
- TDD: off (source: existing feature docs `segmento-venta-endpoint.md` and `segmento-tests.md`). Runner: `dotnet test`.
- Tests never touch real infrastructure (no SQL Server, no Keycloak).
- Not a git repository: work-unit commits are not possible.
- Known schema smell (not changed here): `Fecha`/`Hora` are `VARCHAR(50)`.

## Tasks
- [x] T1 — Domain + Infraestructure: entity, `ConflictException`, repository method with transactional INSERT + UPDATE + duplicate check. Route: delegated (writer trigger, 2+ non-trivial files).
- [x] T2 — Application: request DTO, validator, mapping, service method. Route: delegated (same writer).
- [x] T3 — Api: controller action + middleware 409 mapping. Route: delegated (same writer).
- [x] T4 — Tests: validator, service, controller (201/400/401/404/409). Route: delegated (same writer).
- [x] T5 — Verify: `dotnet build` + `dotnet test` green. Route: writer runs, parent spot check.
- [x] T6 — Remove duplicate check: user confirmed N results per `ContactId + InteractionId` are valid business data. Drop the exists query, `ConflictException`, 409 mapping/attributes/docs and the 409 tests; re-verify build + tests. Route: delegated (writer trigger, 2+ files).
- [x] T7 — Keep the insert when no gestión matches: user confirmed the GAIA result must always persist. UPDATE 0 rows -> commit anyway, log a warning, return 201 with `{ Id, GestionActualizada = false }`. No 404 on this path. Route: delegated (writer trigger, 2+ files).
- [x] T8 — Drop `gestionActualizada` from the POST response (user: not needed by GAIA). Response is `{ id }`; the internal flag stays in `RegistroResultadoGaia` only to drive the warning log. Removed the controller 201-false test, flag assertions in service tests. Verified: `dotnet build` 0 warnings/0 errors, `dotnet test` 58/58. Route: inline (mechanical, already-understood edits).
- [x] T9 — Merge `ISegmentoGaiaRepository`/`SegmentoGaiaRepository` into `ISegmentoRepository`/`SegmentoRepository` (user: keep a single repository). Update DI, `SegmentoService` constructor, tests. Route: inline (mechanical move). Verified: `dotnet build` 0 warnings/0 errors, `dotnet test` 58/58; no references to `SegmentoGaiaRepository` remain.
- [x] T10 — Controller tests cover every documented POST response: added 400 over-length (service not called), 500 BusinessException (generic message), 500 unexpected exception. Verified: `dotnet test` 61/61. Route: inline (one test file).

## Acceptance criteria
- A valid request inserts one row and, when a matching `GSS_Gestiones` row exists, updates it atomically in the same transaction.
- Repeated requests for the same `ContactId + InteractionId` each insert their own row (no conflict).
- **[T7 update]** A missing management record no longer returns 404: the insert is always committed, the endpoint returns 201 with `GestionActualizada = false`, and a warning is logged with `ContactId`/`InteractionId`.
- Invalid body returns 400, missing token returns 401.

## Progress / Evidence

**Files created:**
- `PacificoSegmentoVentasGaia.Domain/Entities/SegmentoGaia.cs` — entity mirroring `GSS_SegmentoGaia` columns except `Id`/`FechaCreacion`.
- `PacificoSegmentoVentasGaia.Domain/Exceptions/ConflictException.cs` — mirrors `NotFoundException`, `BusinessException` subclass.
- `PacificoSegmentoVentasGaia.Domain/Interfaces/ISegmentoGaiaRepository.cs`
- `PacificoSegmentoVentasGaia.Infraestructure/Repositories/SegmentoGaiaRepository.cs`
- `PacificoSegmentoVentasGaia.Application/Dtos/SegmentoGaiaResultadoRequestDto.cs`
- `PacificoSegmentoVentasGaia.Application/Dtos/SegmentoGaiaResultadoResponseDto.cs`
- `PacificoSegmentoVentasGaia.Application/Validators/SegmentoGaiaResultadoRequestValidator.cs`
- `PacificoSegmentoVentasGaia.Tests/Application/Validators/SegmentoGaiaResultadoRequestValidatorTests.cs`

**Files modified:**
- `PacificoSegmentoVentasGaia.Infraestructure/DependencyInjection.cs` — registers `ISegmentoGaiaRepository`.
- `PacificoSegmentoVentasGaia.Application/Mappings/SegmentoMappingProfile.cs` — `CreateMap<SegmentoGaiaResultadoRequestDto, SegmentoGaia>()`.
- `PacificoSegmentoVentasGaia.Application/Interfaces/ISegmentoService.cs` — added `RegistrarResultadoGaiaAsync`.
- `PacificoSegmentoVentasGaia.Application/Services/SegmentoService.cs` — added `ISegmentoGaiaRepository` dependency + method implementation.
- `PacificoSegmentoVentasGaia.Api/Controllers/SegmentoController.cs` — `POST api/segmento/resultado` action (Swagger annotations for 201/400/401/404/409/500).
- `PacificoSegmentoVentasGaia.Api/Middleware/ExceptionHandlingMiddleware.cs` — `ConflictException` → 409 via `ApiErrorResponse` (same envelope as 404).
- `PacificoSegmentoVentasGaia.Api/Program.cs` — Swagger description bullet list now lists 201/409.
- `PacificoSegmentoVentasGaia.Tests/Application/Services/SegmentoServiceTests.cs` — `CreateService` now takes an optional `ISegmentoGaiaRepository`; 4 new tests (map+call+returns id, Conflict propagation, NotFound propagation, CancellationToken forwarded).
- `PacificoSegmentoVentasGaia.Tests/Api/Controllers/SegmentoControllerTests.cs` — 5 new integration tests (401, 400 missing ids, 201 with id, 404, 409), reusing the existing `SegmentoServiceFake` since both methods live on `ISegmentoService`.

**Design choices made within the fixed decisions:**
- Repository throws `ConflictException`/`NotFoundException` directly from inside the transaction (chosen over returning an enum/result the service interprets), since the repo is the only place that knows *why* — duplicate vs. missing gestión — at the exact moment the transaction must roll back. The Polly "db" pipeline's `ShouldHandle` only catches `SqlException`(transient codes)/`TimeoutException`, so it never retries these domain exceptions — satisfying "must not retry a duplicate/not-found business outcome" without extra code.
- Transaction rollback is implicit: `using var transaction = connection.BeginTransaction();` with no explicit `Commit()` on the exception paths. `SqlTransaction.Dispose()` rolls back automatically if `Commit()` was never called, so throwing `ConflictException`/`NotFoundException` before `Commit()` is sufficient and avoids double-rollback bugs from a catch-all handler.
- Connection is opened explicitly and asynchronously (`DbConnection.OpenAsync`, falling back to sync `Open()` for any other `IDbConnection` implementation) because `BeginTransaction()` requires an already-open connection, unlike the existing `SegmentoRepository.GetSegmentoVentaAsync`, which lets Dapper auto-open for a single non-transactional query.
- Existing `SegmentoService`/`SegmentoController` were extended (new constructor dependency, new interface method, new controller action) rather than creating a parallel `SegmentoGaiaService`/controller, per "action on existing SegmentoController" / "service method" (singular) in scope.
- 409 reuses `ApiErrorResponse` (per decision #3). Left `ErrorResponseExamplesSchemaFilter` unchanged: it keys its example message by response *type*, and 409 shares the exact same type as 404 (`ApiErrorResponse`), so there is no separate slot to add a distinct 409 example without introducing a new type, which the decisions didn't call for.
- `SegmentoGaiaResultadoResponseDto` (`{ Id }`) is constructed directly in the service rather than via AutoMapper, since it's a single scalar with no entity counterpart worth a mapping rule.

**Pre-existing inconsistency noticed (not in scope, not changed):** `NotFoundException`'s XML doc says "Ningún endpoint actual la lanza" (no current endpoint throws it), but `SegmentoService.GetSegmentoVentaAsync` already throws it today. The doc comment was already stale before this change.

### T6 — Duplicate check removed

**Files modified:**
- `PacificoSegmentoVentasGaia.Infraestructure/Repositories/SegmentoGaiaRepository.cs` — removed the `existsSql` (`WITH (UPDLOCK, HOLDLOCK)`) query, the `yaExiste` check and the `ConflictException` throw; the resiliency comment above the pipeline call no longer mentions "duplicado". The transaction now goes straight from `BeginTransaction()` to the INSERT.
- `PacificoSegmentoVentasGaia.Domain/Exceptions/ConflictException.cs` — deleted (no longer thrown anywhere).
- `PacificoSegmentoVentasGaia.Api/Middleware/ExceptionHandlingMiddleware.cs` — removed the `case ConflictException` branch (409 mapping).
- `PacificoSegmentoVentasGaia.Api/Controllers/SegmentoController.cs` — removed the `[ProducesResponseType(..., Status409Conflict)]` and `[SwaggerResponse(Status409Conflict, ...)]` attributes on `RegistrarResultadoGaiaAsync`.
- `PacificoSegmentoVentasGaia.Api/Program.cs` — removed the `409 – Resultado GAIA duplicado` bullet from the Swagger description.
- `PacificoSegmentoVentasGaia.Application/Interfaces/ISegmentoService.cs` — XML doc on `RegistrarResultadoGaiaAsync` no longer references `ConflictException`; now states multiple results per ContactId + InteractionId are allowed.
- `PacificoSegmentoVentasGaia.Domain/Interfaces/ISegmentoGaiaRepository.cs` — same XML doc update on `RegistrarResultadoAsync`.
- `PacificoSegmentoVentasGaia.Tests/Application/Services/SegmentoServiceTests.cs` — removed `RegistrarResultadoGaiaAsync_RepositoryThrowsConflictException_PropagatesConflictException`.
- `PacificoSegmentoVentasGaia.Tests/Api/Controllers/SegmentoControllerTests.cs` — removed `ResultadoGaia_ServiceThrowsConflictException_Returns409WithFailureBody`.

Grepped the whole solution (excluding `bin`/`obj`/`.vs`) for `ConflictException`, `409`, `Status409Conflict`, `duplicad` after the change — no remaining references in source; `PacificoSegmentoVentasGaia.Api/Swagger/ErrorResponseExamplesSchemaFilter.cs` never referenced 409/Conflict, so it needed no change.

### T7 — GAIA result always persisted (no rollback / no 404 when gestión is missing)

**Files created:**
- `PacificoSegmentoVentasGaia.Domain/Entities/RegistroResultadoGaia.cs` — `public record RegistroResultadoGaia(int Id, bool GestionActualizada)`, the repository's new return type.

**Files modified:**
- `PacificoSegmentoVentasGaia.Infraestructure/Repositories/SegmentoGaiaRepository.cs` — `RegistrarResultadoAsync` now returns `Task<RegistroResultadoGaia>`. The `if (filasAfectadas == 0) throw new NotFoundException(...)` branch was removed; `transaction.Commit()` now always runs and the method returns `new RegistroResultadoGaia(id, GestionActualizada: filasAfectadas > 0)`. Removed the now-unused `using PacificoSegmentoVentasGaia.Domain.Exceptions;`. Updated the resiliency/rollback comments: the pipeline retry comment no longer claims a business exception can be thrown; the `using`-without-`Commit()` comment now says rollback happens if a `SqlException` propagates before `Commit()`, not "if the business exception is thrown".
- `PacificoSegmentoVentasGaia.Domain/Interfaces/ISegmentoGaiaRepository.cs` — `RegistrarResultadoAsync` signature and XML doc updated: no longer documents throwing `NotFoundException`; documents that the result is always persisted and `GestionActualizada` reflects whether the gestión was found.
- `PacificoSegmentoVentasGaia.Application/Interfaces/ISegmentoService.cs` — XML doc on `RegistrarResultadoGaiaAsync` updated to match (no exception thrown; `GestionActualizada = false` + warning log instead).
- `PacificoSegmentoVentasGaia.Application/Services/SegmentoService.cs` — added `ILogger<SegmentoService>` constructor dependency (new last parameter, `Microsoft.Extensions.Logging`). `RegistrarResultadoGaiaAsync` reads the `RegistroResultadoGaia` from the repository, logs `LogWarning` with `SegmentoGaiaId`/`ContactId`/`InteractionId` as structured properties when `GestionActualizada` is `false`, and returns `SegmentoGaiaResultadoResponseDto { Id, GestionActualizada }`.
- `PacificoSegmentoVentasGaia.Application/Dtos/SegmentoGaiaResultadoResponseDto.cs` — added `public bool GestionActualizada { get; set; }` with XML doc.
- `PacificoSegmentoVentasGaia.Api/Controllers/SegmentoController.cs` — removed `[ProducesResponseType(..., Status404NotFound)]` and the `[SwaggerResponse(Status404NotFound, ...)]` line from `RegistrarResultadoGaiaAsync` only (the GET action keeps its 404). Updated the action's `<remarks>`/`<returns>` and the `SwaggerOperation.Description` to state the result is always persisted and that `gestionActualizada` can be `false`.
- `PacificoSegmentoVentasGaia.Api/Program.cs` — the Swagger description's `404` bullet said "Segmento de venta o gestión no encontrada" (covering both the GET's and the former POST's 404); since POST no longer 404s, it now reads "Segmento de venta no encontrado".
- `PacificoSegmentoVentasGaia.Tests/Application/Services/SegmentoServiceTests.cs` — `CreateService` now passes `NullLogger<SegmentoService>.Instance` as the new `ILogger` dependency (added `using Microsoft.Extensions.Logging.Abstractions;`, removed the now-unused `using NSubstitute.ExceptionExtensions;`). `RegistrarResultadoGaiaAsync_MapsRequestFieldsAndCallsRepository_ReturnsId` and `..._ForwardsCancellationTokenToRepository` now stub `RegistroResultadoGaia` instead of a bare `int`. Replaced `RegistrarResultadoGaiaAsync_RepositoryThrowsNotFoundException_PropagatesNotFoundException` with two tests: `..._RepositoryReturnsGestionActualizadaTrue_ResponseReflectsIt` and `..._RepositoryReturnsGestionActualizadaFalse_ResponseReflectsItWithoutThrowing`.
- `PacificoSegmentoVentasGaia.Tests/Api/Controllers/SegmentoControllerTests.cs` — replaced `ResultadoGaia_ServiceThrowsNotFoundException_Returns404WithFailureBody` with `ResultadoGaia_ServiceReturnsGestionActualizadaFalse_Returns201WithGestionActualizadaFalse`, asserting `201 Created` and `gestionActualizada: false` in the response body. The GET's own 404 test (`GetSegmento_ServiceThrowsNotFoundException_Returns404WithFailureBody`) is untouched.

**Design choices made within the fixed decisions:**
- `RegistroResultadoGaia` is a `record` (the codebase had no prior records; existing DTOs/entities are all plain classes), matching the task's suggested shape `(int Id, bool GestionActualizada)` — a small immutable value pair with no behavior fit a record better than a class or a bare tuple, which would have left the repository interface's public signature untyped.
- Placed `RegistroResultadoGaia.cs` in `Domain/Entities` (next to `SegmentoGaia.cs`), not a new `Domain/Models` folder, since `Domain/Common` (the only other candidate folder) is empty/unused and `Entities` already holds the module's other repository-facing shapes.
- Logging uses `Microsoft.Extensions.Logging.ILogger<SegmentoService>` injected via DI, matching the existing pattern in `ExceptionHandlingMiddleware` (the only other place in the codebase that logs) rather than introducing a new logging abstraction.
- Verified (grep across the whole solution, excluding `bin`/`obj`/`.vs`) that no other XML doc, comment, or Swagger text still claims the POST action returns 404; the only remaining `404`/`NotFound` references in `SegmentoController.cs` belong to the GET action.

## Verification
- Pre-T6: `dotnet build PacificoSegmentoVentasGaia.sln`: **Compilación correcta. 0 Advertencia(s). 0 Errores.**
- Pre-T6: `dotnet test PacificoSegmentoVentasGaia.sln`: **Correctas! - Con error: 0, Superado: 60, Omitido: 0, Total: 60.**
- Post-T6: `dotnet build PacificoSegmentoVentasGaia.sln`: **Compilación correcta. 0 Advertencia(s). 0 Errores.**
- Post-T6: `dotnet test PacificoSegmentoVentasGaia.sln`: **Correctas! - Con error: 0, Superado: 58, Omitido: 0, Total: 58.** (60 → 58: the two removed Conflict/409 tests.)
- Post-T7: `dotnet build PacificoSegmentoVentasGaia.sln`: **Compilación correcta. 0 Advertencia(s). 0 Errores.**
- Post-T7: `dotnet test PacificoSegmentoVentasGaia.sln`: **Correctas! - Con error: 0, Superado: 59, Omitido: 0, Total: 59.** (58 → 59: removed 1 NotFound-propagation service test, added 2 GestionActualizada true/false service tests, removed 1 404 controller test, added 1 201/GestionActualizada=false controller test.)

## Status
Done.

## Next step
None — feature complete. Possible follow-up (out of scope, not actioned): fix the stale `NotFoundException` XML doc comment noted above.
