using Microsoft.AspNetCore.Identity;

namespace InsuranceClaimsSystem.Models;

/// <summary>
/// Represents an application user with extended properties for insurance claims management.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the company name associated with the user.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the department of the user.
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user role (Admin, Manager, Employee, Assessor).
    /// </summary>
    public string Role { get; set; } = "Employee";

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the user was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the user was last modified.
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this user.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for insurance claims created by this user.
    /// </summary>
    public ICollection<InsuranceClaim> CreatedClaims { get; set; } = new List<InsuranceClaim>();

    /// <summary>
    /// Navigation property for insurance claims assigned to this assessor.
    /// </summary>
    public ICollection<InsuranceClaim> AssignedClaims { get; set; } = new List<InsuranceClaim>();
}
