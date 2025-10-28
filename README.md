# Readify ??

Readify is a full-stack bookstore management application built with **ASP.NET Core (.NET 8)** for the backend and **Angular 17** for the frontend. This repository contains a local, cost-free simulation mode where emails are logged to the database and the application runs fully locally.

## Features
- JWT authentication + refresh tokens
- Role-based access (Admin / User)
- Product catalog with search, filtering, sorting, and pagination
- Image upload (local) and validation
- Profile management (view/edit, change password)
- Email subsystem (logged to DB for simulation)
- xUnit unit/integration tests and Cypress E2E tests
- Dockerfiles and GitHub Actions for CI and image publishing

## Run locally

### Backend
1. Ensure SQL Server LocalDB or SQL Server is available.
2. Update `appsettings.json` connection string if necessary.
3. Run migrations or let the app apply EnsureCreated:

   dotnet tool install --global dotnet-ef --version 8.*
   dotnet ef database update --project Readify --startup-project Readify --context AppDbContext

4. Run the API:

   dotnet run --project Readify

API will listen on http://localhost:5005 by default.

### Frontend

1. Install dependencies and run dev server:

   cd readify-frontend
   npm ci
   npm start

The frontend dev server proxies API requests to `http://localhost:5005` via `proxy.conf.json`.

## Demo accounts (seeded)
- Admin: `admin@demo.com` / `Readify#Demo123!`
- User: `user@demo.com` / `Readify#Demo123!`

## Tests
- Run backend tests: `dotnet test`
- Run frontend E2E: `npx cypress open` (from `readify-frontend`)

## Docker
- Backend: `docker build -f Readify/Dockerfile -t readify-backend .`
- Frontend: `docker build -f readify-frontend/Dockerfile -t readify-frontend .`

## CI
GitHub Actions workflow runs build, tests, and publishes Docker images to Docker Hub when secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` are set.

## Screenshots
(Add screenshots of the app UI and Swagger here)
