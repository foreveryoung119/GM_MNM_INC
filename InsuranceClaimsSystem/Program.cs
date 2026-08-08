using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InsuranceClaimsSystem.Data;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5136");

// Add database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        b => b.MigrationsAssembly("InsuranceClaimsSystem")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add application services
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Add Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Logout");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Privacy");
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

// Add antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add CORS if needed for future API development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Migrate database and seed identity data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    await SeedRolesAsync(roleManager);
    await MigrateLegacyRolesAsync(roleManager, userManager);
    await SeedAdminUserAsync(userManager, configuration, startupLogger);
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// HttpsRedirection is handled by Nginx in production; only apply in development.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

await app.RunAsync();

static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    var roles = new[] { "Admin", "Insurance Officer", "Assessor", "Broker Company Officer", "Lawyer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

static async Task MigrateLegacyRolesAsync(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager)
{
    // Keep existing users functional after role name changes.
    var usersToMigrate = userManager.Users
        .Where(u => u.Role == "Officer" || u.Role == "Accountant")
        .ToList();

    foreach (var user in usersToMigrate)
    {
        var currentRoles = await userManager.GetRolesAsync(user);

        if (currentRoles.Contains("Officer"))
        {
            await userManager.RemoveFromRoleAsync(user, "Officer");
            if (await roleManager.RoleExistsAsync("Insurance Officer"))
            {
                await userManager.AddToRoleAsync(user, "Insurance Officer");
            }

            user.Role = "Insurance Officer";
        }

        if (currentRoles.Contains("Accountant"))
        {
            await userManager.RemoveFromRoleAsync(user, "Accountant");
            if (await roleManager.RoleExistsAsync("Lawyer"))
            {
                await userManager.AddToRoleAsync(user, "Lawyer");
            }

            user.Role = "Lawyer";
        }

        await userManager.UpdateAsync(user);
    }

    var accountantRole = await roleManager.FindByNameAsync("Accountant");
    if (accountantRole != null)
    {
        await roleManager.DeleteAsync(accountantRole);
    }
}

static async Task SeedAdminUserAsync(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger logger)
{
    var defaultAdminEmail = configuration["SeedAdmin:Email"];
    var defaultAdminPassword = configuration["SeedAdmin:Password"];

    if (string.IsNullOrWhiteSpace(defaultAdminEmail) || string.IsNullOrWhiteSpace(defaultAdminPassword))
    {
        logger.LogInformation("Seed admin skipped. Set SeedAdmin:Email and SeedAdmin:Password to create a default admin account.");
        return;
    }

    var adminUser = await userManager.FindByEmailAsync(defaultAdminEmail);
    if (adminUser != null)
    {
        return;
    }

    adminUser = new ApplicationUser
    {
        UserName = defaultAdminEmail,
        Email = defaultAdminEmail,
        EmailConfirmed = true,
        FullName = "Administrator",
        Role = "Admin",
        IsActive = true,
        CreatedDate = DateTime.UtcNow
    };

    var createResult = await userManager.CreateAsync(adminUser, defaultAdminPassword);
    if (createResult.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
        logger.LogInformation("Default admin user seeded for {AdminEmail}.", defaultAdminEmail);
        return;
    }

    var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
    logger.LogWarning("Failed to seed default admin user for {AdminEmail}. Errors: {Errors}", defaultAdminEmail, errors);
}
