# Insurance Claims Management System - Development Guide

## Project Overview

This is a secure ASP.NET Core web application for managing insurance claims processing with comprehensive user management, document handling, and settlement tracking features.

## Key Technologies

- **Framework**: ASP.NET Core 8.0 with Razor Pages
- **Database**: SQL Server (Entity Framework Core 8.0)
- **Authentication**: ASP.NET Core Identity with JWT support
- **Security**: Password hashing, account lockout, CSRF protection, file upload validation

## Project Structure Reference

```
InsuranceClaimsSystem/
├── Models/              # Domain entities (ApplicationUser, InsuranceClaim, ClaimDocument, ClaimSettlement)
├── Data/                # ApplicationDbContext and database configuration
├── Services/            # Business logic services (ClaimService, UserService, DocumentService)
├── Pages/               # Razor Pages (Admin, Claims, Shared layouts)
├── Migrations/          # Entity Framework migrations
├── wwwroot/             # Static assets and uploads directory
└── Program.cs           # Application startup and dependency injection
```

## Development Workflow

### Adding New Features

1. Create model in `Models/` directory
2. Update `ApplicationDbContext.cs` with DbSet and configuration
3. Create migration: `dotnet ef migrations add FeatureName`
4. Create service interface in `Services/IXxxService.cs`
5. Implement service in `Services/XxxService.cs`
6. Create Razor Pages in `Pages/`
7. Build and test: `dotnet build && dotnet run`

### Database Management

- **Create Migration**: `dotnet ef migrations add MigrationName`
- **Apply Migration**: `dotnet ef database update`
- **Remove Migration**: `dotnet ef migrations remove`
- **Drop Database**: `dotnet ef database drop --force`

### Security Guidelines

- Always validate user input
- Use parameterized queries (EF Core does this)
- Hash passwords using Identity framework
- Implement role-based authorization
- Validate file uploads (type, size, content)
- Use HTTPS in production
- Keep dependencies updated

## Common Tasks

### Running the Application

```bash
cd InsuranceClaimsSystem
dotnet run
# Navigate to https://localhost:5001
```

### Creating a New Service

1. Create interface: `Services/INewService.cs`
2. Implement service: `Services/NewService.cs`
3. Register in Program.cs: `builder.Services.AddScoped<INewService, NewService>();`

### Adding Database Seeding

Edit `Program.cs` in the migration application section to seed initial data:

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    // Add seeding logic here
}
```

### Debugging Issues

- Check logs in `appsettings.json` logging configuration
- Use breakpoints in Visual Studio
- Check database migrations with: `dotnet ef migrations list`
- Verify connection string in appsettings.json

## Testing Recommendations

### Unit Test Setup

```bash
dotnet new xunit -n InsuranceClaimsSystem.Tests
cd InsuranceClaimsSystem.Tests
dotnet add reference ../InsuranceClaimsSystem/InsuranceClaimsSystem.csproj
```

### Test Areas to Cover

- Service business logic (ClaimService, UserService)
- Authentication and authorization
- Document upload validation
- Settlement calculations
- User role permissions

## Production Deployment Checklist

- [ ] Set secure connection strings (use Azure Key Vault)
- [ ] Enable HTTPS with valid SSL certificate
- [ ] Configure database backup strategy
- [ ] Set up logging and monitoring (Application Insights)
- [ ] Enable Azure WAF for API protection
- [ ] Implement rate limiting
- [ ] Configure CORS properly
- [ ] Enable two-factor authentication
- [ ] Perform security audit
- [ ] Set up CI/CD pipeline

## Code Format and Style

- Use PascalCase for public members
- Use camelCase for private members and parameters
- Add XML documentation comments to public types and methods
- Follow Microsoft C# naming conventions
- Keep methods focused and under 30 lines when possible
- Use meaningful variable names

## Important Notes

- Any custom SQL queries should use parameterized queries
- Always include proper error handling and logging
- Validate all user input on both client and server
- Follow the principle of least privilege for user roles
- Document complex business logic
- Test security-sensitive features thoroughly

## Getting Help

- Review existing services for implementation patterns
- Check Entity Framework documentation: https://learn.microsoft.com/ef/
- ASP.NET Core Identity: https://learn.microsoft.com/aspnet/core/security/authentication/identity/
- Security best practices: https://owasp.org/Top10/

---

**Last Updated**: April 3, 2026
**Version**: 1.0.0
