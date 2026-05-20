# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

CheapAnalysisApp — Bulgaria-first personal finance + portfolio dashboard. Combines PSD2 bank sync, IBKR portfolio data, and Anthropic Claude summaries. Source-available under PolyForm Noncommercial 1.0.0.

**Phase 0 is active.** The README's stack section lists the target architecture; most of it is not built yet. Only `backend/` exists in code so far. `frontend/`, `ibkr-sidecar/`, `proto/`, and the `make` targets referenced in the README are planned, not present. Do not assume they exist — check before referencing.

**Stack (target):** .NET 10 + FastEndpoints API, Angular frontend, PostgreSQL, Hangfire + Redis jobs, Python gRPC sidecar for IBKR (`ib_insync`), Anthropic Claude for summaries, Docker Compose + GitHub Actions. (README says ".NET 8" — that is stale. `backend/global.json` pins SDK `10.0.300`, `Directory.Build.props` targets `net10.0`.)

## Backend layout (`backend/`)

Clean-architecture solution `CheapAnalysis.sln` with the standard four-project split + three test projects:

- `CheapAnalysis.Domain` — entities, value objects, no dependencies
- `CheapAnalysis.Application` — use cases, depends on Domain
- `CheapAnalysis.Infrastructure` — EF Core, providers, external integrations
- `CheapAnalysis.Api` — ASP.NET Core host (`Microsoft.NET.Sdk.Web`), references Application + Infrastructure
- `tests/CheapAnalysis.{Unit,Integration,E2E}Tests`

Most csproj files are still empty scaffolds — packages get wired per task (e.g. T-004 adds FastEndpoints/Serilog/NSwag, T-207 adds Hangfire). The csproj `<!-- comments -->` track which task owns each future package addition; preserve them when editing.

## Build settings — important

`backend/Directory.Build.props` applies to every project under `backend/` and turns on:

- `TreatWarningsAsErrors=true` + `EnforceCodeStyleInBuild=true`
- `GenerateDocumentationFile=true` (required so IDE0005 unused-using runs on build — see Roslyn #41640). `CS1591` (missing XML comment) is suppressed so this doesn't force docstrings.
- `AnalysisLevel=latest-recommended`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`

`backend/Directory.Build.targets` relaxes `TreatWarningsAsErrors` for test projects only — it has to live in `.targets` so `$(IsTestProject)` is already set. Don't move that block back into `.props`.

`.editorconfig` enforces file-scoped namespaces, `using`s outside the namespace, no `this.` qualifier, primary constructors preferred, braces required, 4-space C# / 2-space web / tabs in Makefiles. IDE0005 is at `warning` severity but escalates to error via `TreatWarningsAsErrors` — unused usings break the build.

## Common commands

```powershell
# Backend — run from backend/
dotnet restore
dotnet build
dotnet test                                          # all test projects
dotnet test tests/CheapAnalysis.UnitTests            # one project
dotnet test --filter "FullyQualifiedName~SomeClass"  # single test / class
dotnet run --project src/CheapAnalysis.Api           # API host
```

Root `package.json` exists only for commitlint + Husky tooling — no app code lives at the root.

```powershell
npm install                       # at repo root, installs commitlint/husky
npx commitlint --from HEAD~1      # lint last commit
```

## Commits & branches

**Conventional Commits enforced** via `commitlint.config.cjs`:

- Types (error if unknown): `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
- Scopes (warn, not error): `backend`, `frontend`, `sidecar`, `proto`, `db`, `ci`, `docker`, `docs`, `deps`, `auth`, `bank`, `ibkr`, `ai`, `release`
- Subject: not sentence/start/pascal/upper case; no trailing period; header ≤100 chars; body lines ≤100 chars

**Branch naming** (from `CONTRIBUTORS.md`):

- `feature/<task-id>-<slug>` — e.g. `feature/t-004-fastendpoints-host`. Task ID must come from `docs/PROJECT_TRACKER.xlsx` so the branch maps back to a tracker row.
- `fix/<slug>`, `chore/<slug>`, `docs/<slug>`
- Do **not** push auto-generated `claude/<adjective-name-hash>` worktree branch names as the canonical PR branch — rename before opening the PR.

## Planning docs (not in this repo)

`ARCHITECTURE.md` and `PROJECT_TRACKER.xlsx` live untracked at `C:\Users\pavel\PSProductions\CheapAnalysisApp\docs\`. They are deliberately kept local — do not suggest committing them. Task IDs (e.g. T-003, T-004, T-207) referenced in csproj comments and branch names come from the tracker.
