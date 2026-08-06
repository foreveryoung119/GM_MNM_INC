using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Services;

/// <summary>
/// Service interface for managing insurance claims.
/// </summary>
public interface IClaimService
{
    /// <summary>
    /// Creates a new insurance claim.
    /// </summary>
    /// <param name="claim">The insurance claim to create.</param>
    /// <returns>The created insurance claim.</returns>
    Task<InsuranceClaim> CreateClaimAsync(InsuranceClaim claim);

    /// <summary>
    /// Gets all insurance claims.
    /// </summary>
    /// <returns>List of all insurance claims.</returns>
    Task<List<InsuranceClaim>> GetAllClaimsAsync();

    /// <summary>
    /// Gets a specific insurance claim by ID.
    /// </summary>
    /// <param name="claimId">The claim ID.</param>
    /// <returns>The insurance claim or null if not found.</returns>
    Task<InsuranceClaim?> GetClaimByIdAsync(int claimId);

    /// <summary>
    /// Gets claims created by a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of claims created by the user.</returns>
    Task<List<InsuranceClaim>> GetUserClaimsAsync(string userId);

    /// <summary>
    /// Gets claims assigned to a specific assessor.
    /// </summary>
    /// <param name="assessorId">The assessor user ID.</param>
    /// <returns>List of claims assigned to the assessor.</returns>
    Task<List<InsuranceClaim>> GetAssessorClaimsAsync(string assessorId);

    /// <summary>
    /// Updates an existing insurance claim.
    /// </summary>
    /// <param name="claim">The insurance claim with updated information.</param>
    /// <returns>The updated insurance claim.</returns>
    Task<InsuranceClaim> UpdateClaimAsync(InsuranceClaim claim);

    /// <summary>
    /// Updates the claim status.
    /// </summary>
    /// <param name="claimId">The claim ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="modifiedBy">The user making the modification.</param>
    /// <returns>The updated insurance claim.</returns>
    Task<InsuranceClaim> UpdateClaimStatusAsync(int claimId, ClaimStatus status, string modifiedBy);

    /// <summary>
    /// Assigns an assessor to a claim.
    /// </summary>
    /// <param name="claimId">The claim ID.</param>
    /// <param name="assessorId">The assessor user ID.</param>
    /// <param name="assignedBy">The user assigning the assessor.</param>
    /// <returns>The updated insurance claim.</returns>
    Task<InsuranceClaim> AssignAssessorAsync(int claimId, string assessorId, string assignedBy);

    /// <summary>
    /// Generates a unique claim number.
    /// </summary>
    /// <returns>A unique claim number.</returns>
    Task<string> GenerateClaimNumberAsync();

    /// <summary>
    /// Gets claims by status.
    /// </summary>
    /// <param name="status">The claim status.</param>
    /// <returns>List of claims with specified status.</returns>
    Task<List<InsuranceClaim>> GetClaimsByStatusAsync(ClaimStatus status);

    /// <summary>
    /// Gets counts of claims grouped by status.
    /// </summary>
    /// <returns>Dictionary keyed by claim status with counts.</returns>
    Task<Dictionary<ClaimStatus, int>> GetClaimCountsByStatusAsync();
}
