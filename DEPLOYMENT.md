# Installation & Deployment Guide

## Prerequisites

### Development Environment

- .NET 8.0 SDK or later
- Visual Studio 2022 or Visual Studio Code with C# extension
- SQL Server 2019 or later (or SQL Server Express LocalDB)
- Node.js (for front-end tools, if needed)

### System Requirements

- Windows 10/11, macOS, or Linux
- Minimum 4GB RAM
- 2GB free disk space for development
- 10GB for SQL Server installation

## Development Setup

### Step 1: Project Initialization

```bash
# Clone or open the repository
cd InsuranceClaimsSystem

# Restore NuGet packages
dotnet restore

# Verify .NET version
dotnet --version
```

### Step 2: Database Configuration

1. **Configure Connection String**

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InsuranceClaimsSystemDb;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

2. **Alternative Connection Strings**

For SQL Server Express:

```
Server=YOUR_SERVER_NAME;Database=InsuranceClaimsSystemDb;Trusted_Connection=true;
```

For SQL Server with Authentication:

```
Server=YOUR_SERVER_NAME;Database=InsuranceClaimsSystemDb;User Id=sa;Password=YOUR_PASSWORD;
```

### Step 3: Apply Database Migrations

```bash
# Create database and apply migrations
dotnet ef database update

# Verify migrations
dotnet ef migrations list
```

### Step 4: Build and Run

```bash
# Clean previous builds
dotnet clean

# Build the project
dotnet build

# Run the application
dotnet run
```

The application will be available at: `https://localhost:5001`

## Production Deployment

### Deployment Options

#### Option 1: Azure App Service

1. **Prepare for Deployment**

   ```bash
   dotnet publish -c Release -o publish
   ```

2. **Create Azure App Service**

   ```bash
   az appservice plan create --name InsuranceClaimsAppPlan --resource-group myResourceGroup --sku B2
   az webapp create --resource-group myResourceGroup --plan InsuranceClaimsAppPlan --name InsuranceClaimsApp
   ```

3. **Deploy Artifacts**

   ```bash
   az webapp deployment source config-zip --resource-group myResourceGroup --name InsuranceClaimsApp --src publish.zip
   ```

4. **Configure Application Settings**
   - Database connection string
   - Application settings
   - API keys

#### Option 2: IIS on Windows Server

1. **Prepare for IIS**

   ```bash
   dotnet publish -c Release -o iis_publish
   ```

2. **Install Hosting Bundle**
   - Download .NET 8.0 Hosting Bundle from microsoft.com
   - Run installer on target server
   - Restart IIS

3. **Create IIS Website**
   - Open IIS Manager
   - Create new website pointing to published folder
   - Configure application pool for `.NET Core (serverless)`

4. **Set Permissions**
   ```powershell
   icacls C:\iis_publish /grant:r "IIS AppPool\YourAppPoolName:(OI)(CI)F"
   ```

#### Option 3: Docker Container

1. **Create Dockerfile**

   ```docker
   FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
   WORKDIR /app
   COPY bin/Release/net8.0/publish .
   EXPOSE 80
   EXPOSE 443
   ENTRYPOINT ["dotnet", "InsuranceClaimsSystem.dll"]
   ```

2. **Build Docker Image**

   ```bash
   docker build -t insurance-claims:latest .
   ```

3. **Run Container**
   ```bash
   docker run -d -p 80:80 --name insurance-claims insurance-claims:latest
   ```

### Pre-Deployment Checklist

- [ ] Security review completed
- [ ] All unit tests passing
- [ ] Code review approved
- [ ] Database backup strategy configured
- [ ] SSL certificate obtained and configured
- [ ] Logging and monitoring configured
- [ ] Backup and disaster recovery plan in place
- [ ] Performance testing completed
- [ ] Load testing completed
- [ ] Security scanning completed

### Post-Deployment Verification

```bash
# Verify application health
curl -k https://your-domain.com/

# Check database connectivity
# Run database health check query

# Verify logging is working
# Check application logs

# Test authentication
# Attempt login with test credentials
```

## Configuration Management

### Environment Variables

Development:

```bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionString=...
```

Production:

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionString=... # From Azure Key Vault
JWT_KEY=... # From Key Vault
```

### Application Settings by Environment

**development.json** (Development only):

```json
{
  "DetailedErrors": true,
  "Logging": { "LogLevel": { "Default": "Debug" } }
}
```

**appsettings.Production.json** (Production):

```json
{
  "DetailedErrors": false,
  "Logging": { "LogLevel": { "Default": "Information" } }
}
```

## Database Maintenance

### Backup Strategy

**Automated Backups:**

- Daily full backups at 2 AM UTC
- Transaction log backups every 15 minutes
- Retention: 30 days

**Manual Backup:**

```bash
sqlcmd -S YOUR_SERVER -d InsuranceClaimsSystemDb -Q "BACKUP DATABASE [InsuranceClaimsSystemDb] TO DISK = 'C:\backups\backup.bak'"
```

### Database Migration

**To a New Server:**

1. Take backup on source server
2. Restore backup on target server
3. Update connection string
4. Run: `dotnet ef database update --context ApplicationDbContext`

## Monitoring & Logging

### Application Insights (Azure)

1. Create Application Insights resource
2. Add instrumentation key to `appsettings.json`
3. Install NuGet: `Microsoft.ApplicationInsights.AspNetCore`
4. Configure in `Program.cs`:
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

### Health Checks

Create health check endpoint:

```csharp
app.MapHealthChecks("/health");
```

### Log Analysis

Access logs in Azure:

```kusto
traces
| where timestamp > ago(24h)
| summarize count() by severityLevel
```

## Troubleshooting

### Connection String Issues

```bash
# Test connection
sqlcmd -S (localdb)\mssqllocaldb -Q "SELECT 1"

# List local databases
sqllocaldb info
```

### Migration Issues

```bash
# Remove last migration
dotnet ef migrations remove

# Reset database
dotnet ef database drop --force
dotnet ef database update
```

### Permission Issues

```bash
# On Windows
icacls "C:\path\to\app" /grant "DOMAIN\User":(OI)(CI)F /T

# Check current user
whoami
```

## Performance Tuning

### Database Optimization

- Add indexes for frequently queried fields
- Archive old claims data
- Regular statistics updates

### Application Caching

```csharp
builder.Services.AddMemoryCache();
builder.Services.AddDistributedSqlServerCache(options => { });
```

### Connection Pooling

Update connection string:

```
Server=...;Max Pool Size=100;Min Pool Size=10;
```

## Rollback Plan

1. **Version Control**: Always maintain previous version tag in git
2. **Database**: Keep previous schema backup
3. **Plan**:
   - Stop current application
   - Restore previous release
   - Restore previous database backup
   - Verify functionality

## Monitoring Alerts

Set up alerts for:

- Application errors (>5 per minute)
- Database connection failures
- High CPU usage (>80%)
- High memory usage (>85%)
- Response time > 2 seconds
- 5xx errors > 1%

---

**Last Updated**: April 3, 2026
**Version**: 1.0.0
