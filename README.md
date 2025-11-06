# Readify

Readify is a sample e-commerce bookstore application built with ASP.NET Core (backend) and Angular (frontend). It demonstrates a small production-like stack: authenticated API, EF Core persistence, admin dashboards, and a modern SPA frontend.

## Features
- Public book catalog with search, filtering, and pagination
- Shopping cart and checkout flow
- Admin area with product management, orders, users, and dashboard
- JWT authentication and role-based authorization
- Chart.js integration for admin sales charts
- Export features: chart PNG and CSV export

## Tech stack
- Backend: .NET 8 / ASP.NET Core Web API, Entity Framework Core
- Frontend: Angular (standalone components), Angular Material, Chart.js
- Tests: xUnit for backend, Jasmine/Karma for frontend

## Quick local setup
Prerequisites: .NET 8 SDK, Node 18+, npm

1. Clone the repo

   git clone https://github.com/Vk-Viman/Book_Store.git
   cd Readify

2. Backend

   dotnet restore
   dotnet ef database update
   dotnet run --project Readify

   The API runs at `http://localhost:5005` by default. You can reset or seed demo data in development using:

   POST http://localhost:5005/api/test/reset

3. Frontend

   cd readify-frontend
   npm ci
   npm start

   The frontend runs at `http://localhost:4200` and proxies API requests to the backend.

## Sample admin account (local dev)
- Email: admin@example.com
- Password: Password123!

Note: The database initializer seeds an admin user for local development. If you reset the DB using the test reset endpoint the seed will be re-applied.

## Scripts
- Backend tests: `dotnet test Readify.UnitTests/Readify.UnitTests.csproj`
- Frontend build: `cd readify-frontend && npm run build`
- Frontend tests: `cd readify-frontend && npm test`

## Folder structure
- `Readify` - backend API, controllers, data, DTOs
- `readify-frontend` - Angular app
- `Readify.UnitTests` - backend tests

## Contributing
See `CONTRIBUTING.md` for contribution guidelines and test organization.

## Screenshots
(Placeholders — add screenshots for home, catalog, admin dashboard)

## License
MIT
