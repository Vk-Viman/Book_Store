# Readify ??

Readify is a full-stack bookstore management application built with **ASP.NET Core (.NET 8)** for the backend and **Angular 17** for the frontend. This repository contains a local, cost-free simulation mode where emails are logged to the database and the application runs fully locally.

## ?? Features

### Authentication & Security
- JWT authentication with refresh tokens
- Role-based access control (Admin / User)
- Password reset flow with one-time tokens
- Secure password hashing with BCrypt
- Token revocation support

### Book Catalog
- Advanced search and filtering (title, author, category, price range)
- Price range slider with dual-thumb controls
- Multiple sort options (title, price, date)
- Pagination with condensed page numbers
- Category-based browsing
- Real-time filter chips with remove buttons
- Lazy-loaded images with placeholders
- Stock availability indicators

### Admin Features
- **Dashboard** with statistics:
  - Total products, users, categories
  - Recent activity tracking
- Product management (CRUD operations)
- Image upload with validation
- Category management
- Audit logging for all actions

### User Features
- Profile management (view/edit name and email)
- Password change with validation
- Order history (framework ready)
- Personalized experience

### UI/UX Enhancements
- Material Design theme with custom color palette
- Loading skeletons for better perceived performance
- Responsive design (mobile-first)
- Accessibility improvements (ARIA labels, keyboard navigation)
- Empty states with friendly messages
- Toast notifications for user actions
- Password visibility toggles
- Inline form validation

### Technical Features
- Response caching (60s for catalog endpoints)
- Response compression (gzip + brotli)
- Email subsystem (logged to DB for simulation, SMTP-ready)
- Comprehensive audit logging
- Image validation and optimization
- Error handling middleware
- xUnit unit/integration tests
- Cypress E2E tests
- Docker support with multi-stage builds
- CI/CD with GitHub Actions

## ?? Run Locally

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- SQL Server LocalDB or SQL Server

### Backend Setup

1. **Navigate to the backend directory:**
   ```bash
   cd Readify
   ```

2. **Update connection string** in `appsettings.json` if necessary:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ReadifyDb;Trusted_Connection=True"
   }
   ```

3. **Install EF tool (if needed):**
   ```bash
   dotnet tool install --global dotnet-ef --version 8.*
   ```

4. **Apply migrations or let auto-seed:**
   ```bash
   dotnet ef database update --project Readify --context AppDbContext
   # OR just run the app and it will create the database
   ```

5. **Run the API:**
   ```bash
   dotnet run
   ```
   API will be available at `http://localhost:5005`

### Frontend Setup

1. **Navigate to frontend directory:**
   ```bash
   cd readify-frontend
   ```

2. **Install dependencies:**
   ```bash
   npm ci
   ```

3. **Run dev server:**
   ```bash
   npm start
   ```
   App will open at `http://localhost:4200`

The frontend dev server proxies API requests to `http://localhost:5005` via `proxy.conf.json`.

## ?? Demo Accounts (Auto-Seeded)

| Role  | Email             | Password            |
|-------|-------------------|---------------------|
| Admin | admin@demo.com    | Readify#Demo123!    |
| User  | user@demo.com     | Readify#Demo123!    |

## ?? Testing

### Backend Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Frontend E2E Tests
```bash
cd readify-frontend

# Interactive mode
npx cypress open

# Headless mode
npx cypress run
```

## ?? Docker

### Build Images

**Backend:**
```bash
docker build -f Readify/Dockerfile -t readify-backend:latest .
```

**Frontend:**
```bash
docker build -f readify-frontend/Dockerfile -t readify-frontend:latest .
```

### Run with Docker Compose
```bash
# Coming soon - docker-compose.yml
```

## ?? Architecture

### Backend Stack
- **Framework:** ASP.NET Core 8.0 Web API
- **ORM:** Entity Framework Core 8
- **Database:** SQL Server
- **Authentication:** JWT Bearer tokens
- **Validation:** FluentValidation & Data Annotations
- **Testing:** xUnit, WebApplicationFactory
- **Documentation:** Swagger/OpenAPI

### Frontend Stack
- **Framework:** Angular 17 (standalone components)
- **UI Library:** Angular Material + Bootstrap 5
- **State Management:** RxJS Observables
- **HTTP Client:** Angular HttpClient with interceptors
- **Routing:** Angular Router with lazy loading
- **Testing:** Cypress E2E
- **Build:** Angular CLI with esbuild

### Key Design Patterns
- Repository pattern (via DbContext)
- Dependency injection
- Middleware pipeline
- JWT refresh token rotation
- Audit logging interceptor
- Clean separation of concerns (Controllers ? Services ? Data)

## ?? Configuration

### Backend (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ReadifyDb;..."
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-characters",
    "Issuer": "Readify",
    "Audience": "ReadifyUsers"
  },
  "Smtp": {
    "Enabled": false,
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "",
    "Password": "",
    "From": "",
    "FromDisplayName": "Readify"
  },
  "Storage": {
    "RootPath": "wwwroot",
    "ImagesPath": "images",
    "MaxImageSizeBytes": 2097152
  }
}
```

### Frontend (environment.ts)

```typescript
export const environment = {
  apiUrl: 'http://localhost:5005/api'
};
```

## ?? Phase-by-Phase Development

### Phase 1 - MVP
- Basic CRUD for books and categories
- User authentication (register/login)
- Admin product management
- Simple catalog listing

### Phase 2 - Security & Robustness
- JWT refresh tokens
- Role-based authorization
- Password reset flow
- Error handling middleware
- Input validation

### Phase 3 - UX & Features
- Advanced filtering and search
- Image upload with validation
- Profile management
- Pagination and sorting
- Responsive UI

### Phase 4 - Production Readiness
- Email notifications (SMTP-ready, logging mode)
- Response caching and compression
- Audit logging expansion
- Unit and E2E tests
- Docker support
- CI/CD pipeline

### Phase 5 - Polish & Optimization
- Material Design implementation
- Loading skeletons
- Price range slider
- Admin dashboard
- Enhanced forms with validation
- Accessibility improvements

## ?? Project Structure

```
Readify/
??? Readify/                    # Backend API
?   ??? Controllers/           # API endpoints
?   ??? Services/              # Business logic
?   ??? Models/                # Domain entities
?   ??? DTOs/                  # Data transfer objects
?   ??? Data/                  # DbContext & migrations
?   ??? Helpers/               # JWT, mapping utilities
?   ??? Middleware/            # Error handling
?   ??? Program.cs             # App configuration
??? Readify.Tests/             # Backend tests
??? readify-frontend/          # Angular app
?   ??? src/app/
?   ?   ??? components/       # Reusable UI components
?   ?   ??? pages/            # Route components
?   ?   ??? services/         # HTTP & business services
?   ?   ??? guards/           # Route guards
?   ?   ??? interceptors/     # HTTP interceptors
?   ??? cypress/              # E2E tests
??? .github/workflows/         # CI/CD
??? README.md
```

## ?? Deployment

### CI/CD Pipeline (GitHub Actions)
- Automated builds on push/PR
- Runs backend tests
- Runs frontend E2E tests
- Builds & pushes Docker images to Docker Hub

### Environment Variables (Secrets)
- `DOCKERHUB_USERNAME`
- `DOCKERHUB_TOKEN`
- Database connection strings
- JWT secret keys
- SMTP credentials (if enabled)

## ?? Security Best Practices

? Passwords hashed with BCrypt  
? JWT tokens with expiration  
? Refresh token rotation  
? SQL injection prevention (EF parameterized queries)  
? XSS protection (Angular sanitization)  
? CORS policy configured  
? HTTPS enforcement ready  
? Input validation on both client and server  
? Role-based authorization  
? Audit logging for sensitive actions  

## ?? Performance Optimizations

- Response caching (catalog endpoints)
- Response compression (gzip/brotli)
- Lazy loading routes (Angular)
- Image lazy loading
- Code splitting (Angular chunks)
- Database indexes on frequently queried fields
- Debounced search/filter inputs

## ?? Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## ?? License

This project is for educational/portfolio purposes.

## ?? Contributing

This is a personal portfolio project, but feedback and suggestions are welcome!

## ?? Contact

For questions or feedback, please open an issue on GitHub.

---

**Built with ?? using ASP.NET Core and Angular**
