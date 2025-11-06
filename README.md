# Readify — Run Locally (no hosting)

This repository contains the Readify sample app (ASP.NET Core backend + Angular frontend). The project is intentionally *not* published to GitHub Pages or any hosted environment by default. The instructions below show how to run everything locally for development and testing.

## Prerequisites
- .NET 8 SDK
- Node.js 20+ and npm
- SQL Server LocalDB or a running SQL Server instance (or Docker if you prefer)
- Optional: `dotnet-ef` tool (for EF migrations)

## Backend (API) — Run locally
1. Open a terminal at the repository root.
2. Update configuration (optional): edit `appsettings.Development.json` or `appsettings.json` to set your connection string. Example LocalDB connection:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ReadifyDb;Trusted_Connection=True"
   }
   ```

3. (Optional) Install EF CLI if you plan to run migrations manually:

   ```bash
   dotnet tool install --global dotnet-ef --version 8.*
   ```

4. Apply migrations (creates or updates the DB schema):

   ```bash
   cd Readify
   dotnet ef database update
   ```

   The app also runs `DbInitializer` on startup which will seed demo data if the DB is empty.

5. Run the backend API:

   ```bash
   cd Readify
   dotnet run
   ```

   The API listens on the URLs shown in the console (default: `http://localhost:5005`).

6. Useful endpoints
   - Swagger UI (development): `http://localhost:5005/swagger`
   - Test helper (development only): `POST /api/test/reset` will reset DB and re-run seeding.

## Frontend (Angular) — Run locally
1. Install dependencies and start dev server:

   ```bash
   cd readify-frontend
   npm ci
   npm start
   ```

   The frontend dev server runs at `http://localhost:4200` and proxies API requests to the backend (see `proxy.conf.json`).

2. If you need to change the API endpoint the frontend calls, update `readify-frontend/src/environments/environment.ts` (or set a replacement during build).

3. To run frontend and backend at the same time (single command), the project includes a `dev` script that runs both processes concurrently:

   ```bash
   cd readify-frontend
   npm run dev
   ```

   This runs the backend and the Angular dev server together.

## Running Tests
- Backend unit/integration tests (xUnit):

  ```bash
  cd Readify.Tests
  dotnet test
  ```

  To collect coverage locally:

  ```bash
  dotnet test --collect:"XPlat Code Coverage"
  ```

- Frontend unit tests (Karma/Jasmine):

  ```bash
  cd readify-frontend
  npm test
  ```

## Database and Seeding
- `DbInitializer` seeds demo users and products during startup when running in Development mode.
- Use `POST /api/test/reset` (only enabled in Development environment) to force a DB reset and reseed — useful for running tests from a clean state.

## Common Troubleshooting
- SQL Server not accessible: confirm `DefaultConnection` is correct and SQL Server or LocalDB is running.
- EF migrations mismatch: if `dotnet ef database update` reports pending changes, run `dotnet ef migrations add <Name>` then update, or revert model changes.
- Port conflicts: backend default port may differ; check `dotnet run` output and adjust `environment.apiUrl` in the frontend if needed.

## Notes about Deployment
- GitHub Pages serves only static sites. The ASP.NET Core backend cannot be hosted on GitHub Pages.
- This repository previously had CI workflows for testing. No automatic publishing to Pages or any host is configured by default in this repo.
- If you decide later to publish the frontend only, add a GitHub Actions workflow to build the Angular app and publish the `dist` directory to a `gh-pages` branch.

## Quick Start Summary
1. Ensure prerequisites are installed.
2. Configure connection string (if necessary).
3. From `Readify/`, run migrations then `dotnet run`.
4. From `readify-frontend/`, run `npm ci` and `npm start` (or `npm run dev` for both).
5. Run tests with `dotnet test` and `npm test` as needed.

---
If you want, I can also:
- Add a short shell/batch script to start both backend and frontend with one command.
- Remove any CI workflows that attempt to deploy the app (CI test workflow remains useful).
