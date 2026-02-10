# Repository Guidelines

## Project Structure & Module Organization
- `src/Timecard.Api/`: ASP.NET Core minimal API (`Program.cs`), domain rules (`Domain/`), data access and EF Core context (`Data/`), and endpoint groups (`Features/Adjustments`, `Features/Days`, `Features/Month`, `Features/Punch`).
- `src/Timecard.Api/wwwroot/`: static frontend files (`index.html`, `app.js`, `api.js`, `styles.css`).
- `tests/Timecard.Tests/`: xUnit tests for domain/business behavior (for example `WorkRulesTests.cs`).
- `Timecard.slnx`: solution entry point for loading/building the full repo.

## Build, Test, and Development Commands
- `dotnet restore Timecard.slnx`: restore NuGet dependencies.
- `dotnet build Timecard.slnx`: compile API and tests.
- `dotnet run --project src/Timecard.Api/Timecard.Api.csproj`: run locally (default `http://localhost:5077`).
- `dotnet test tests/Timecard.Tests/Timecard.Tests.csproj`: run unit tests.
- Optional EF workflow when schema evolves: `dotnet ef migrations add <Name> --project src/Timecard.Api` then `dotnet ef database update`.

## Coding Style & Naming Conventions
- C# uses nullable reference types and implicit usings (enabled in project files).
- Use 4-space indentation and keep files UTF-8.
- Public types/members: `PascalCase`; local variables/parameters: `camelCase`; async methods: `*Async`.
- Keep endpoint mapping organized by feature folder and extension methods (for example `MapPunchEndpoints`).
- Frontend JS/CSS in `wwwroot/` should stay framework-free and modular (`api.js`, feature-specific logic, shared styles).

## Encoding Rules (Critical)
- All text files must be UTF-8. Prefer UTF-8 without BOM when writing via scripts.
- Never use PowerShell `Set-Content` or `Out-File` without an explicit encoding.
- PowerShell writes must use one of these patterns:
  - `Set-Content -Path <file> -Value <content> -Encoding UTF8`
  - `[System.IO.File]::WriteAllText(<file>, <content>, [System.Text.UTF8Encoding]::new($false))`
- If CJK text is involved, prefer `[System.IO.File]::ReadAllText(..., [System.Text.Encoding]::UTF8)` and `WriteAllText(..., UTF8Encoding($false))` to avoid mojibake.

## Testing Guidelines
- Framework: xUnit (`[Fact]` tests).
- Name tests as `MethodOrScenario_Condition_ExpectedResult` (e.g., `Workday_PositiveDelta_CapsAt55`).
- Add tests for rule changes in `Timecard.Api/Domain/WorkRules.cs` and API behavior changes where relevant.
- Run tests before opening a PR: `dotnet test`.

## Commit & Pull Request Guidelines
- Follow existing commit style: concise imperative subject, often with a tag prefix such as `[add]`, `[refactor]`, or `[rev]`.
- Keep commits focused (one logical change per commit) and include related test updates.
- PRs should include: purpose summary, key design/behavior changes, test evidence (`dotnet test` result), and API/UI screenshots for visible frontend changes.
- Link related issues/tasks and call out any DB/schema impact explicitly.
