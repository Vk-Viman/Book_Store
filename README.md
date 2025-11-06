Readify — Run Locally (no hosting)

This repository contains the Readify sample app (ASP.NET Core backend + Angular frontend). The project is intentionally not published to GitHub Pages or any hosted environment by default. The instructions below show how to run everything locally for development and testing.

CI note: The repository CI is configured to run only pure unit tests (no integration or E2E tests). Integration tests that require SQL Server or external services were removed from automated CI to keep the pipeline lightweight for portfolio use.

Prerequisites
- .NET 8 SDK
- Node.js 20+ and npm
- SQL Server LocalDB or a running SQL Server instance (or Docker if you prefer)
- Optional: dotnet-ef tool (for EF migrations)

Backend (API) - Run locally
1. Open a terminal at the repository root.
2. Update configuration (optional): edit appsettings.Development.json or appsettings.json to set your connection string. Example LocalDB connection:

   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ReadifyDb;Trusted_Connection=True"
     }
   }

3. (Optional) Install EF CLI if you plan to run migrations manually:

   dotnet tool install --global dotnet-ef --version 8.*

4. Apply migrations (creates or updates the DB schema):

   cd Readify
   dotnet ef database update

   The app also runs DbInitializer on startup which will seed demo data if the database is empty.

5. Run the backend API:

   cd Readify
   dotnet run

   The API listens on the URLs shown in the console (default: http://localhost:5005).

6. Useful endpoints
- Swagger UI (development): http://localhost:5005/swagger
- Test helper (development only): POST /api/test/reset will reset the database and re-run seeding.

Frontend (Angular) - Run locally
1. Install dependencies and start the dev server:

   cd readify-frontend
   npm ci
   npm start

   The frontend dev server runs at http://localhost:4200 and proxies API requests to the backend (see proxy.conf.json).

2. If you need to change the API endpoint the frontend calls, update readify-frontend/src/environments/environment.ts or set a replacement during build.

3. To run frontend and backend at the same time (single command), the project includes a dev script that runs both processes concurrently:

   cd readify-frontend
   npm run dev

Running tests
- Unit tests (xUnit):

  dotnet test Readify.UnitTests/Readify.UnitTests.csproj

  To collect coverage locally:

  dotnet test --collect:"XPlat Code Coverage"

- Frontend unit tests (Karma/Jasmine):

  cd readify-frontend
  npm test

Database and seeding
- DbInitializer seeds demo users and products during startup when running in Development mode.
- Use POST /api/test/reset (only enabled in Development environment) to force a database reset and reseed.

Common troubleshooting
- SQL Server not accessible: confirm DefaultConnection is correct and SQL Server or LocalDB is running.
- EF migrations mismatch: if dotnet ef database update reports pending changes, run dotnet ef migrations add <Name> then update, or revert model changes.
- Port conflicts: backend default port may differ; check dotnet run output and adjust environment.apiUrl in the frontend if needed.

Notes about deployment
- GitHub Pages serves only static sites. The ASP.NET Core backend cannot be hosted on GitHub Pages.
- This repository is configured as a portfolio artifact and CI is intentionally limited to unit tests.

Quick start summary
1. Ensure prerequisites are installed.
2. Configure connection string (if necessary).
3. From Readify/, run migrations then dotnet run.
4. From readify-frontend/, run npm ci and npm start (or npm run dev for both).
5. Run unit tests with dotnet test and npm test as needed.

If you want, I can also add a short script to start backend and frontend together. If you prefer, I can remove any remaining test files or adjust test projects further.
