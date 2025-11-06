# Contributing to Readify

Thanks for your interest in contributing. This document explains how tests are organized and how to run them locally.

## Tests
- Backend: xUnit tests live in `Readify.UnitTests`.
  - Run `dotnet test Readify.UnitTests/Readify.UnitTests.csproj` to execute the unit and integration tests.
- Frontend: Jasmine/Karma specs live under `readify-frontend/src/app` alongside components/services.
  - Run `npm test` from `readify-frontend` to execute frontend specs.

## Adding tests
- Backend tests should avoid coupling to a live SQL Server. Use the `TestHelpers` utilities and in-memory or SQLite provider.
- Frontend tests should mock HTTP calls with `HttpClientTestingModule`.

## Pull request process
- Fork the repository, make your changes on a feature branch, and open a Pull Request against `master`.
- Include unit tests for any new behavior.

## Style
- Follow C# and TypeScript idioms consistent with the project.
- Keep component templates small and extract shared logic to services.

