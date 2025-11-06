Readify - Run Locally

This repository contains the Readify sample application (ASP.NET Core backend + Angular frontend).

Note: CI is configured to run unit tests only. Integration and E2E tests are not run in CI.

Quick start (local):

1. Backend:
   - cd Readify
   - dotnet restore
   - dotnet ef database update
   - dotnet run

2. Frontend:
   - cd readify-frontend
   - npm ci
   - npm start

3. Tests:
   - dotnet test Readify.UnitTests/Readify.UnitTests.csproj

This README file is plain ASCII/UTF-8 to avoid encoding issues with static site builders.
