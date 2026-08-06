# Quick Start Guide

## 5-Minute Setup

### Prerequisites

- .NET 8.0 SDK (download from dotnet.microsoft.com)
- Visual Studio 2022, VS Code, or any C# IDE
- SQL Server LocalDB (included with Visual Studio) or SQL Server Express

### Quick Setup Steps

```bash
# 1. Navigate to project
cd InsuranceClaimsSystem

# 2. Restore packages
dotnet restore

# 3. Create database
dotnet ef database update

# 4. Build project
dotnet build

# 5. Run application
dotnet run
```

**Application will be available at**: `https://localhost:5001`

## File Locations Reference

| Component      | Location    | File                                                                        |
| -------------- | ----------- | --------------------------------------------------------------------------- |
| Models         | `Models/`   | ApplicationUser.cs, InsuranceClaim.cs, ClaimDocument.cs, ClaimSettlement.cs |
| Database       | `Data/`     | ApplicationDbContext.cs                                                     |
| Business Logic | `Services/` | ClaimService.cs, UserService.cs, DocumentService.cs                         |
| Pages          | `Pages/`    | Admin/, Claims/, Shared/                                                    |
| Configuration  | Root        | appsettings.json, Program.cs                                                |
| Docs           | Root        | README.md, SECURITY.md, DEPLOYMENT.md                                       |

## Key Concepts

### Claim Status Flow

```
ClaimIntimation
  ↓
AssessorAppointed
  ↓
DocumentsRequested
  ↓
DocumentsSubmitted
  ↓
UnderNegotiation
  ↓
DischargeVoucherPrepared
  ↓
PaymentReleased
  ↓
Closed
```

### User Roles

- **Admin**: System administration, user management
- **Insurance Officer**: Claims oversight, intake, and assessor appointment
- **Assessor**: Evaluate assigned claims, verify documents
- **Broker Company Officer**: Broker-side claim follow-up and documentation
- **Lawyer**: Legal review and payment release governance

## Common Commands

```bash
# Create new database migration
dotnet ef migrations add MigrationName

# View all migrations
dotnet ef migrations list

# Undo last migration
dotnet ef migrations remove

# Reset database (WARNING: Deletes all data)
dotnet ef database drop --force

# Clean build
dotnet clean && dotnet build

# Run with detailed logging
dotnet run --verbosity=diagnostic
```

## Debugging Tips

### Check Database Connection

```bash
# Verify LocalDB is running
sqllocaldb info

# List local databases
sqllocaldb query
```

### View Application Logs

Check `appsettings.json` log level configuration:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",  // Change to "Debug" for verbose logging
    "Microsoft.AspNetCore": "Warning"
  }
}
```

### Common Issues

| Issue                             | Solution                                                      |
| --------------------------------- | ------------------------------------------------------------- |
| Database connection fails         | Ensure LocalDB service is running: `sqllocaldb start`         |
| Build error: Package not found    | Run `dotnet restore`                                          |
| "Add-Migration" command not found | Install: `dotnet tool install --global dotnet-ef`             |
| Port 5001 already in use          | Change port in `launchSettings.json` or kill existing process |

## Project Structure Overview

```
InsuranceClaimsSystem/
├── Models/                 # Database entities
├── Data/                   # EF Core context
├── Services/              # Business logic
├── Pages/                 # Razor Pages (UI)
├── Migrations/            # Database migrations
├── wwwroot/              # Static files
├── Program.cs            # App startup
├── appsettings.json      # Configuration
└── InsuranceClaimsSystem.csproj
```

## Adding New Claims Feature

1. **Create Model** in `Models/NewEntity.cs`
2. **Add DbSet** to `ApplicationDbContext`
3. **Create Migration**: `dotnet ef migrations add AddNewFeature`
4. **Create Service Interface** in `Services/INewService.cs`
5. **Implement Service** in `Services/NewService.cs`
6. **Register Service** in `Program.cs`: `builder.Services.AddScoped<INewService, NewService>();`
7. **Create Pages** in `Pages/NewFeature/`

## Database Connection Strings

### Local Development (LocalDB)

```
Server=(localdb)\mssqllocaldb;Database=InsuranceClaimsSystemDb;Trusted_Connection=true;
```

### SQL Server Express

```
Server=COMPUTER_NAME\SQLEXPRESS;Database=InsuranceClaimsSystemDb;Trusted_Connection=true;
```

### SQL Server with Username/Password

```
Server=SERVER_IP;Database=InsuranceClaimsSystemDb;User Id=sa;Password=YourPassword;
```

## IDE Setup

### Visual Studio 2022

1. Open `InsuranceClaimsSystem.sln`
2. Tools → NuGet Package Manager → Restore Packages
3. Press F5 to run

### Visual Studio Code

1. Open `InsuranceClaimsSystem` folder
2. Ensure C# extension installed
3. Open terminal and run: `dotnet run`

### Command Line

```bash
cd InsuranceClaimsSystem
dotnet run
```

## Testing Services

### Test ClaimService

```csharp
var claimService = serviceProvider.GetRequiredService<IClaimService>();
var newClaim = new InsuranceClaim
{
    IncidentDescription = "Test",
    CreatedById = "user-id"
};
await claimService.CreateClaimAsync(newClaim);
```

### Test UserService

```csharp
var userService = serviceProvider.GetRequiredService<IUserService>();
var user = new ApplicationUser
{
    Email = "test@example.com",
    FullName = "Test User"
};
await userService.CreateUserAsync(user, "SecurePassword123!");
```

## Performance Tips

1. **Use asynchronous operations**: Always use `async/await`
2. **Optimize queries**: Load related data with `.Include()`
3. **Add database indexes**: For frequently searched fields
4. **Cache frequently accessed data**: Use `IMemoryCache`
5. **Limit result sets**: Use pagination for large datasets

## Security Reminders

✅ Always validate user input
✅ Use parameterized queries (EF Core does this automatically)
✅ Hash passwords (Identity framework handles this)
✅ Check user roles before sensitive operations
✅ Log important security events
✅ Never commit secrets to version control
✅ Always use HTTPS in production

## Documentation Reference

| Document                        | Purpose                         |
| ------------------------------- | ------------------------------- |
| README.md                       | Project overview and features   |
| SECURITY.md                     | Security policies and practices |
| DEPLOYMENT.md                   | Deployment instructions         |
| .github/copilot-instructions.md | Developer guidelines            |
| IMPLEMENTATION_SUMMARY.md       | What was built                  |

## Getting Help

1. Check **README.md** for common questions
2. Review **SECURITY.md** for security concerns
3. See **DEPLOYMENT.md** for deployment issues
4. Check Entity Framework documentation: https://learn.microsoft.com/ef/
5. ASP.NET Core docs: https://learn.microsoft.com/aspnet/core/

---

**Last Updated**: April 3, 2026
**Version**: 1.0.0
