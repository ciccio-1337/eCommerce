# CLAUDE.md

## Stack
- .NET 10 solution (`eCommerce.sln`). Two products:
  - Storefront: layered (`eCommerce.Storefront.Model`, `Repository.EntityFrameworkCore`, `Services`, `Services.Cache`, `Controllers`, `UI.Web.MVC`), tests in `eCommerce.Storefront.Tests`.
  - Backoffice: Blazor (WASM client `eCommerce.Backoffice.Client`, server `eCommerce.Backoffice.Server`, shared `eCommerce.Backoffice.Shared`).
- `global.json` uses `Microsoft.Testing.Platform` test runner.
- `Directory.Build.props` adds `SQLitePCLRaw.bundle_e_sqlite3` to all projects.
- `Directory.Build.targets` suppresses the WASM workload warning for `eCommerce.Backoffice.Client` only.

## Commands (use these)
- Build (Release): `./build.command` or `dotnet build -c Release`
- Run storefront: `./run.command` or `dotnet run -c Release --project eCommerce.Storefront.UI.Web.MVC/eCommerce.Storefront.UI.Web.MVC.csproj`
- Build+run: `./build_and_run.command`
- Migrations (EF Core): `./add_migration.command`, `./update_database.command`, `./remove_latest_migration.command` (run from repo root as written)
- Package updates: `./update_outdated_packages.command`
- Docker: `./docker.command`; `Dockerfile` copies `eCommerce.Storefront.UI.Web.MVC/bin/Release/net10.0/publish/` and runs `dotnet eCommerce.Storefront.UI.Web.MVC.dll`

## Build/Test
- CI (.github/workflows/dotnet-core.yml): `dotnet restore` → `dotnet build --configuration Release --no-restore` → `dotnet test --no-restore --verbosity normal`
- Tests: `eCommerce.Storefront.Tests` uses MSTest 4.4.0 with `EnableMSTestRunner` and `OutputType Exe`. Run all tests: `dotnet test` (uses Microsoft.Testing.Platform per `global.json`). To run a single test/project: `dotnet test eCommerce.Storefront.Tests/eCommerce.Storefront.Tests.csproj` or filter as MSTest supports.
- If building/publishing for Docker, produce publish output in `bin/Release/net10.0/publish/` for the MVC project (Dockerfile expects that path).

## Architecture notes
- Storefront startup (`eCommerce.Storefront.UI.Web.MVC/Program.cs`): calls `AddInfrastructure`, `AddRepositories`, `AddApplicationServices`, adds MVC with Newtonsoft.Json and custom model binder/filter setup, uses localization (en-GB/en-US/it-IT), Blazor framework files/static files, routing/auth, `UseSecurityHeaders()`, maps default route and fallback to `index.html`. Exception handling differs dev vs prod.
- Backoffice is a separate Blazor app (WASM client) hosted via `eCommerce.Backoffice.Server` with `eCommerce.Backoffice.Shared`.

## Gotchas
- Backoffice.Client pulls transitive SQLite native assets but doesn't use them; the custom target avoids a noisy WASM workload warning without enabling AOT/native build changes.
- `SQLitePCLRaw.bundle_e_sqlite3` is referenced globally via `Directory.Build.props` (present for all projects).
- Docker entrypoint assumes published storefront app; build/publish it before `docker build` or use the provided flow.
