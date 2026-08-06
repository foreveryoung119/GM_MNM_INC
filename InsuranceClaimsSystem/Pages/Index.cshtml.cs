using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;

namespace InsuranceClaimsSystem.Pages;

[Authorize(Roles = "Insurance Officer,Assessor,Lawyer,Admin")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IClaimService _claimService;

    public int TotalClaims { get; set; }
    public int PendingClaims { get; set; }
    public int ProcessingClaims { get; set; }
    public int FinalizedClaims { get; set; }
    public int CancelledClaims { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IClaimService claimService)
    {
        _logger = logger;
        _claimService = claimService;
    }

    public async Task OnGetAsync()
    {
        var statusCounts = await _claimService.GetClaimCountsByStatusAsync();

        TotalClaims = statusCounts.Values.Sum();
        PendingClaims = statusCounts.GetValueOrDefault(ClaimStatus.ClaimIntimation);
        ProcessingClaims = statusCounts.GetValueOrDefault(ClaimStatus.AssessorAppointed) +
                           statusCounts.GetValueOrDefault(ClaimStatus.DocumentsRequested) +
                           statusCounts.GetValueOrDefault(ClaimStatus.DocumentsSubmitted) +
                           statusCounts.GetValueOrDefault(ClaimStatus.UnderNegotiation) +
                           statusCounts.GetValueOrDefault(ClaimStatus.DischargeVoucherPrepared) +
                           statusCounts.GetValueOrDefault(ClaimStatus.PaymentReleased);
        FinalizedClaims = statusCounts.GetValueOrDefault(ClaimStatus.Closed) +
                          statusCounts.GetValueOrDefault(ClaimStatus.Rejected);
        CancelledClaims = statusCounts.GetValueOrDefault(ClaimStatus.Cancelled);
    }
}
