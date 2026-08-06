using Microsoft.EntityFrameworkCore;
using InsuranceClaimsSystem.Data;
using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Services;

/// <summary>
/// Service for managing insurance claims.
/// </summary>
public class ClaimService : IClaimService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClaimService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimService"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="logger">The logger.</param>
    public ClaimService(ApplicationDbContext context, ILogger<ClaimService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<InsuranceClaim> CreateClaimAsync(InsuranceClaim claim)
    {
        try
        {
            claim.ClaimNumber = await GenerateClaimNumberAsync();
            claim.CreatedDate = DateTime.UtcNow;

            _context.InsuranceClaims.Add(claim);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Claim created with number: {claim.ClaimNumber}");
            return claim;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating claim: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<InsuranceClaim>> GetAllClaimsAsync()
    {
        try
        {
            return await _context.InsuranceClaims
                .Include(c => c.CreatedBy)
                .Include(c => c.AssessedBy)
                .Include(c => c.BrokerUser)
                .Include(c => c.Documents)
                .Include(c => c.Settlements)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving all claims: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<ClaimStatus, int>> GetClaimCountsByStatusAsync()
    {
        try
        {
            return await _context.InsuranceClaims
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving claim counts by status: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<InsuranceClaim?> GetClaimByIdAsync(int claimId)
    {
        try
        {
            return await _context.InsuranceClaims
                .Include(c => c.CreatedBy)
                .Include(c => c.AssessedBy)
                .Include(c => c.BrokerUser)
                .Include(c => c.Documents)
                .Include(c => c.Settlements)
                .FirstOrDefaultAsync(c => c.Id == claimId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving claim {claimId}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<InsuranceClaim>> GetUserClaimsAsync(string userId)
    {
        try
        {
            return await _context.InsuranceClaims
                .Where(c => c.CreatedById == userId)
                .Include(c => c.CreatedBy)
                .Include(c => c.AssessedBy)
                .Include(c => c.Documents)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving user claims for {userId}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<InsuranceClaim>> GetAssessorClaimsAsync(string assessorId)
    {
        try
        {
            return await _context.InsuranceClaims
                .Where(c => c.AssessedById == assessorId)
                .Include(c => c.CreatedBy)
                .Include(c => c.AssessedBy)
                .Include(c => c.Documents)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving assessor claims for {assessorId}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<InsuranceClaim> UpdateClaimAsync(InsuranceClaim claim)
    {
        try
        {
            claim.ModifiedDate = DateTime.UtcNow;
            _context.InsuranceClaims.Update(claim);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Claim {claim.Id} updated successfully");
            return claim;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating claim {claim.Id}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<InsuranceClaim> UpdateClaimStatusAsync(int claimId, ClaimStatus status, string modifiedBy)
    {
        try
        {
            var claim = await _context.InsuranceClaims.FindAsync(claimId);
            if (claim == null)
                throw new ArgumentException($"Claim with ID {claimId} not found");

            claim.Status = status;
            claim.ModifiedDate = DateTime.UtcNow;
            claim.ModifiedBy = modifiedBy;

            // Update status-specific dates
            switch (status)
            {
                case ClaimStatus.AssessorAppointed:
                    claim.AssessorAppointedDate = DateTime.UtcNow;
                    break;
                case ClaimStatus.DocumentsRequested:
                    claim.DocumentRequestDate = DateTime.UtcNow;
                    break;
                case ClaimStatus.DocumentsSubmitted:
                    claim.DocumentSubmissionDate = DateTime.UtcNow;
                    break;
                case ClaimStatus.UnderNegotiation:
                    claim.NegotiationStartDate = DateTime.UtcNow;
                    break;
                case ClaimStatus.DischargeVoucherPrepared:
                    claim.DischargeVoucherDate = DateTime.UtcNow;
                    break;
                case ClaimStatus.PaymentReleased:
                    claim.PaymentReleasedDate = DateTime.UtcNow;
                    break;
            }

            _context.InsuranceClaims.Update(claim);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Claim {claimId} status updated to {status}");
            return claim;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating claim status: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<InsuranceClaim> AssignAssessorAsync(int claimId, string assessorId, string assignedBy)
    {
        try
        {
            var claim = await _context.InsuranceClaims.FindAsync(claimId);
            if (claim == null)
                throw new ArgumentException($"Claim with ID {claimId} not found");

            claim.AssessedById = assessorId;
            claim.Status = ClaimStatus.AssessorAppointed;
            claim.AssessorAppointedDate = DateTime.UtcNow;
            claim.ModifiedDate = DateTime.UtcNow;
            claim.ModifiedBy = assignedBy;

            _context.InsuranceClaims.Update(claim);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Assessor {assessorId} assigned to claim {claimId}");
            return claim;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error assigning assessor to claim: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GenerateClaimNumberAsync()
    {
        try
        {
            var year = DateTime.Now.Year;
            var lastClaim = await _context.InsuranceClaims
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastClaim != null && lastClaim.ClaimNumber.Contains(year.ToString()))
            {
                var lastNumberPart = lastClaim.ClaimNumber.Split('-').Last();
                if (int.TryParse(lastNumberPart, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return $"CLM-{year}-{nextNumber:D4}";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating claim number: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<InsuranceClaim>> GetClaimsByStatusAsync(ClaimStatus status)
    {
        try
        {
            return await _context.InsuranceClaims
                .Where(c => c.Status == status)
                .Include(c => c.CreatedBy)
                .Include(c => c.AssessedBy)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving claims by status: {ex.Message}");
            throw;
        }
    }
}
