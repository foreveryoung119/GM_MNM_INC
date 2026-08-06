using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Data;

/// <summary>
/// Application database context for the insurance claims management system.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the insurance claims DbSet.
    /// </summary>
    public DbSet<InsuranceClaim> InsuranceClaims { get; set; } = null!;

    /// <summary>
    /// Gets or sets the claim documents DbSet.
    /// </summary>
    public DbSet<ClaimDocument> ClaimDocuments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the claim settlements DbSet.
    /// </summary>
    public DbSet<ClaimSettlement> ClaimSettlements { get; set; } = null!;

    /// <summary>
    /// Configures the model for the database context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Id).HasMaxLength(64);
            entity.Property(u => u.UserName).HasMaxLength(64);
            entity.Property(u => u.NormalizedUserName).HasMaxLength(64);
            entity.Property(u => u.Email).HasMaxLength(64);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(64);
        });

        modelBuilder.Entity<IdentityRole>(entity =>
        {
            entity.Property(r => r.Id).HasMaxLength(64);
            entity.Property(r => r.Name).HasMaxLength(64);
            entity.Property(r => r.NormalizedName).HasMaxLength(64);
        });

        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(l => l.LoginProvider).HasMaxLength(64);
            entity.Property(l => l.ProviderKey).HasMaxLength(64);
        });

        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(t => t.LoginProvider).HasMaxLength(64);
            entity.Property(t => t.Name).HasMaxLength(64);
        });

        // Configure InsuranceClaim entity
        modelBuilder.Entity<InsuranceClaim>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ClaimNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.IncidentDescription)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.ClaimType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ClaimTypeOther)
                .HasMaxLength(200);

            entity.Property(e => e.ReportedPersonName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.EstimatedAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.SettledAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Remarks)
                .HasMaxLength(1000);

            entity.Property(e => e.DischargeVoucherNumber)
                .HasMaxLength(50);

            entity.Property(e => e.ProofOfPaymentNumber)
                .HasMaxLength(50);

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedClaims)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssessedBy)
                .WithMany(u => u.AssignedClaims)
                .HasForeignKey(e => e.AssessedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BrokerUser)
                .WithMany()
                .HasForeignKey(e => e.BrokerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Documents)
                .WithOne(d => d.InsuranceClaim)
                .HasForeignKey(d => d.InsuranceClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Settlements)
                .WithOne(s => s.InsuranceClaim)
                .HasForeignKey(s => s.InsuranceClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ClaimNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedById);
            entity.HasIndex(e => e.AssessedById);
            entity.HasIndex(e => e.BrokerUserId);
        });

        // Configure ClaimDocument entity
        modelBuilder.Entity<ClaimDocument>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.FileExtension)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.MimeType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.VerificationRemarks)
                .HasMaxLength(500);

            entity.HasOne(e => e.InsuranceClaim)
                .WithMany(c => c.Documents)
                .HasForeignKey(e => e.InsuranceClaimId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne(e => e.VerifiedBy)
                .WithMany()
                .HasForeignKey(e => e.VerifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.InsuranceClaimId);
            entity.HasIndex(e => e.DocumentType);
        });

        // Configure ClaimSettlement entity
        modelBuilder.Entity<ClaimSettlement>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProposedAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.ApprovedAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.RejectionReason)
                .HasMaxLength(500);

            entity.Property(e => e.DischargeVoucherNumber)
                .HasMaxLength(50);

            entity.Property(e => e.ProofOfPaymentNumber)
                .HasMaxLength(50);

            entity.Property(e => e.Remarks)
                .HasMaxLength(500);

            entity.HasOne(e => e.InsuranceClaim)
                .WithMany(c => c.Settlements)
                .HasForeignKey(e => e.InsuranceClaimId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasIndex(e => e.InsuranceClaimId);
            entity.HasIndex(e => e.Status);
        });

        // Configure ApplicationUser entity
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CompanyName)
                .HasMaxLength(200);

            entity.Property(e => e.Department)
                .HasMaxLength(100);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Employee");

            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
