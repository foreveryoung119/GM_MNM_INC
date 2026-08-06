# Insurance Claims Management System

A secure, enterprise-grade ASP.NET Core web application for managing insurance claims processing with user management, document handling, and settlement tracking.

## Features

### User Management Module

- User registration and authentication with secure password policies
- Role-based access control (Admin, Insurance Officer, Assessor, Broker Company Officer, Lawyer)
- User profile management
- Activity logging and audit trails
- User activation/deactivation

### Insurance Claim Management Process

#### 1. **Claim Intimation**

- Report loss/damage to insurer via the system
- Automatic claim number generation (CLM-YYYY-XXXX format)
- Document initial claim details

#### 2. **Assessor Appointment**

- Assign assessor to evaluate loss
- Track assessor assignment date
- Assessor-specific dashboard with assigned claims

#### 3. **Document Management**

- Request required documents from company
- Secure document uploads with validation
- Support for multiple document types (invoices, photos, reports, etc.)
- Document verification and approval workflow
- Maximum file size: 10 MB
- Supported formats: PDF, DOC, DOCX, JPG, JPEG, PNG, GIF, XLS, XLSX

#### 4. **Documents Submission**

- Company submits documents to assessor/insurer
- Document verification by authorized personnel
- Document tracking with timestamps

#### 5. **Settlement Negotiation**

- Negotiate claim settlement amount
- Track negotiation timeline
- Support multiple settlement proposals

#### 6. **Discharge Voucher (DV)**

- Prepare discharge voucher for approved claims
- DV reference number tracking
- Status tracking through approval workflow

#### 7. **Payment Processing & Proof of Payment (POP)**

- Process and release payment
- Generate proof of payment documentation
- Final claim closure

## Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server (LocalDB for development, production deployment ready)
- **ORM**: Entity Framework Core 8.0
- **Authentication**: ASP.NET Core Identity with JWT bearer token support
- **UI**: Razor Pages
- **Security Features**:
  - Password hashing with PBKDF2
  - Account lockout after 5 failed attempts
  - CSRF protection via AntiForgery tokens
  - Session management with 30-minute timeout
  - Role-based authorization
  - Secure file upload validation

## Project Structure

```
InsuranceClaimsSystem/
├── Models/
│   ├── ApplicationUser.cs          # Extended Identity user model
│   ├── InsuranceClaim.cs          # Main claim entity
│   ├── ClaimDocument.cs           # Document management
│   └── ClaimSettlement.cs         # Settlement tracking
├── Data/
│   └── ApplicationDbContext.cs    # EF Core DbContext
├── Services/
│   ├── IClaimService.cs           # Claim service interface
│   ├── ClaimService.cs            # Claim business logic
│   ├── IUserService.cs            # User service interface
│   ├── UserService.cs             # User management logic
│   ├── IDocumentService.cs        # Document service interface
│   └── DocumentService.cs         # Document handling logic
├── Pages/
│   ├── Admin/                     # Administrative pages
│   ├── Claims/                    # Claim management pages
│   └── Shared/                    # Shared layout components
├── Migrations/                    # EF Core migrations
├── Program.cs                     # Application startup
├── appsettings.json              # Application configuration
└── wwwroot/                       # Static files & uploads
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server or SQL Server Express (LocalDB)
- Visual Studio 2022 or Visual Studio Code

### Installation

1. **Clone or open the project**:

   ```bash
   cd InsuranceClaimsSystem
   ```

2. **Restore NuGet packages**:

   ```bash
   dotnet restore
   ```

3. **Update database connection** in `appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Your-SQL-Server-Connection-String"
   }
   ```

4. **Apply database migrations**:

   ```bash
   dotnet ef database update
   ```

5. **Build the project**:

   ```bash
   dotnet build
   ```

6. **Run the application**:

   ```bash
   dotnet run
   ```

   The application will be available at `https://localhost:5001`

## Database Schema

### Key Tables

- **AspNetUsers**: Extended application users
- **InsuranceClaims**: Main claims table with status tracking
- **ClaimDocuments**: Uploaded documents with verification status
- **ClaimSettlements**: Settlement records with negotiation details

## Security Considerations

### Implemented Security Features

- **Password Policy**:
  - Minimum 8 characters
  - Requires uppercase, lowercase, digits, and special characters
  - Account lockout after 5 failed attempts
  - 15-minute lockout duration

- **File Upload Security**:
  - File type validation (whitelist of allowed extensions)
  - File size limit (10 MB)
  - Unique filename generation using GUID
  - Stored separately from web root during development

- **Authentication & Authorization**:
  - Role-based access control
  - Session management with timeout
  - CSRF protection
  - Logging of security events

### Production Deployment Recommendations

1. Use HTTPS with valid SSL certificate
2. Configure SQL Server with database encryption
3. Implement Azure Key Vault for sensitive configuration
4. Enable Azure Application Insights for monitoring
5. Implement API rate limiting
6. Set up automated backups
7. Use Azure managed identity for database connections
8. Enable Web Application Firewall (WAF)
9. Implement two-factor authentication
10. Regular security audits and penetration testing

## API Endpoints (Future)

When API support is added, the following endpoints will be available:

- `GET /api/claims` - Get all claims
- `POST /api/claims` - Create new claim
- `GET /api/claims/{id}` - Get claim details
- `PUT /api/claims/{id}` - Update claim
- `POST /api/claims/{id}/documents` - Upload document
- `GET /api/claims/{id}/documents` - List claim documents
- `POST /api/settlements` - Create settlement record

## Configuration

### Application Settings (appsettings.json)

```json
{
  "ApplicationSettings": {
    "MaxFileSizeInMB": 10,
    "AllowedFileExtensions": [
      ".pdf",
      ".doc",
      ".docx",
      ".jpg",
      ".jpeg",
      ".png",
      ".gif",
      ".xlsx",
      ".xls"
    ],
    "DocumentUploadPath": "uploads",
    "SessionTimeoutMinutes": 30
  }
}
```

## Logging

The application uses built-in .NET logging. Logs are configured in `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore.Database.Command": "Debug"
  }
}
```

## Testing

### Unit Testing

To add unit tests, create a separate test project:

```bash
dotnet new xunit -n InsuranceClaimsSystem.Tests
dotnet add reference ../InsuranceClaimsSystem/InsuranceClaimsSystem.csproj
```

## Troubleshooting

### Database Connection Issues

- Verify SQL Server is running
- Check connection string in appsettings.json
- Ensure localdb service is available: `sqllocaldb info`

### Migration Issues

- Delete Migrations folder and re-create: `dotnet ef migrations add InitialCreate`
- Reset database: `dotnet ef database drop --force`

### File Upload Issues

- Ensure 'wwwroot/uploads' directory exists and has proper permissions
- Check file size doesn't exceed 10 MB limit
- Verify file extension is in allowed list

## License

This project is proprietary software for [Company Name]. Unauthorized copying, modification, or distribution is prohibited.

## Support

For support and issues, contact the development team.

## Changelog

### Version 1.0.0 (Initial Release)

- Core insurance claims management system
- User management module
- Document upload and verification
- Settlement tracking
- Security implementation

---

**Last Updated**: April 3, 2026
**Version**: 1.0.0
