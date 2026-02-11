# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Personal timecard system (MVP) for tracking work hours with flex-time banking. Built with .NET 10, ASP.NET Core minimal APIs, PostgreSQL (via EF Core/Npgsql), and a vanilla JS frontend in Traditional Chinese.

Core business rules: 9-hour planned workday (540 min), daily flex cap of ±55 minutes, monthly flex bank that resets each month. Logic lives in `src/Timecard.Api/Domain/WorkRules.cs`.

## Commands

```bash
dotnet build Timecard.slnx                                    # Build solution
dotnet test tests/Timecard.Tests/Timecard.Tests.csproj        # Run all tests
dotnet test tests/Timecard.Tests/Timecard.Tests.csproj --filter "FullyQualifiedName~ClassName"  # Run specific test class
dotnet run --project src/Timecard.Api/Timecard.Api.csproj     # Run API (http://localhost:5077)
dotnet ef migrations add <Name> --project src/Timecard.Api    # Add EF migration
```

## Architecture

**Domain-Driven Design with Result pattern** — no exceptions for validation. All domain operations return `DomainResult` or `DomainResult<T>` (defined in `Data/DomainResult.cs`). Endpoints convert errors via `ToErrorResult()` extension.

**Aggregate root**: `WorkDay` owns `PunchEvent` and `AttendanceRequest` collections. All mutations go through `WorkDay` methods which enforce invariants (overlap checks, gap detection, minimum punch intervals).

**Project layout** (`src/Timecard.Api/`):
- `Domain/` — Pure business logic (`WorkRules.cs`: flex computation, day/month calculations)
- `Data/` — EF Core context (`TimecardDb`), repository (`WorkDayRepository`), entities, result types
- `Features/` — Minimal API endpoint groups (`Punch/`, `Days/`, `AttendanceRequests/`, `Month/`), each with a `Map*Endpoints()` extension method
- `wwwroot/` — Vanilla JS frontend (framework-free)

**Database**: PostgreSQL. `DateOnly`/`TimeOnly` stored as text via EF value converters. Cascade deletes from WorkDay to children.

## Conventions

- **Error handling**: Return `DomainResult.Fail("message")` — never throw for domain validation
- **Test naming**: `MethodOrScenario_Condition_ExpectedResult` (e.g., `Workday_PositiveDelta_CapsAt55`)
- **Commit messages**: Concise imperative with tag prefix: `[add]`, `[refactor]`, `[remove]`, `[replace]`, `[rev]`
- **Encoding**: All files UTF-8. In PowerShell, always specify encoding explicitly — never bare `Set-Content`/`Out-File`. Use `[System.IO.File]::WriteAllText(path, content, [System.Text.UTF8Encoding]::new($false))` for CJK text
- **C#**: Nullable reference types enabled, implicit usings, latest language version. Records for DTOs and computed results
