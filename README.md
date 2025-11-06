Readify — Run Locally (no hosting)

This repository contains the Readify sample app (ASP.NET Core backend + Angular frontend).

CI note: The repository CI is configured to run only pure unit tests (no integration or E2E tests).

Prerequisites
- .NET 8 SDK
- Node.js 20+ and npm
- SQL Server LocalDB or a running SQL Server instance (or Docker if you prefer)
- Optional: dotnet-ef tool (for EF migrations)

Backend (API) - Run locally
1. Open a terminal at the repository root.
2. Update configuration if needed: edit `appsettings.Development.json` or `appsettings.json` to set your connection string.

Example LocalDB connection:

```
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ReadifyDb;Trusted_Connection=True"
}
```

3. (Optional) Install EF CLI if you plan to run migrations manually:

```
dotnet tool install --global dotnet-ef --version 8.*
```

4. Apply migrations (creates or updates the DB schema):

```
cd Readify
dotnet ef database update
```

5. Run the backend API:

```
cd Readify
dotnet run
```

Default URL: http://localhost:5005

Frontend (Angular) - Run locally
1. Install dependencies and start the dev server:

```
cd readify-frontend
npm ci
npm start
```

2. To run frontend and backend together:

```
cd readify-frontend
npm run dev
```

Unit tests
- Run unit tests (repository-level):

```
dotnet test Readify.UnitTests/Readify.UnitTests.csproj
```

Notes
- This README is saved in UTF-8 encoding to avoid build errors with Jekyll/GitHub Pages.
- The repo is intended as a portfolio artifact and is not published by default.
