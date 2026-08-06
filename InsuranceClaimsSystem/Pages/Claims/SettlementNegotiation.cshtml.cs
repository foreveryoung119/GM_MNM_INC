using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;
using System.Security.Claims;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Insurance Officer,Broker Company Officer,Admin")]
    public class SettlementNegotiationModel : PageModel
    {
        private readonly IClaimService _claimService;

        public InsuranceClaim? Claim { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public SettlementStatus CurrentNegotiationStatus { get; set; } = SettlementStatus.UnderNegotiation;

        public bool CanRejectSettlement => User.IsInRole("Insurance Officer") || User.IsInRole("Admin");

        public string CurrentNegotiationStatusText => CurrentNegotiationStatus switch
        {
            SettlementStatus.Approved => "Settlement Agreed",
            SettlementStatus.Rejected => "Settlement Rejected",
            _ => "Under Negotiation"
        };

        public string CurrentNegotiationStatusCssClass => CurrentNegotiationStatus switch
        {
            SettlementStatus.Approved => "status-agreed",
            SettlementStatus.Rejected => "status-rejected",
            _ => "status-under-negotiation"
        };

        public SettlementNegotiationModel(IClaimService claimService)
        {
            _claimService = claimService;
        }

        public async Task<IActionResult> OnGetAsync(int claimId)
        {
            Claim = await _claimService.GetClaimByIdAsync(claimId);
            if (Claim == null)
            {
                return NotFound();
            }

            if (!CanAccessClaim(Claim))
            {
                return Forbid();
            }

            CurrentNegotiationStatus = Claim.Settlements
                .OrderByDescending(settlement => settlement.CreatedDate)
                .Select(settlement => settlement.Status)
                .DefaultIfEmpty(SettlementStatus.UnderNegotiation)
                .FirstOrDefault();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            int claimId,
            DateTime NegotiationDate,
            decimal ProposedSettlementAmount,
            decimal AgreedSettlementAmount,
            string NegotiationRemarks)
        {
            if (User.IsInRole("Broker Company Officer"))
            {
                return Forbid();
            }

            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            if (!CanAccessClaim(claim))
            {
                return Forbid();
            }

            return await SaveNegotiationAsync(
                claim,
                claimId,
                NegotiationDate,
                ProposedSettlementAmount,
                AgreedSettlementAmount,
                NegotiationRemarks,
                SettlementStatus.UnderNegotiation);
        }

        public async Task<IActionResult> OnPostApproveAsync(
            int claimId,
            DateTime NegotiationDate,
            decimal ProposedSettlementAmount,
            decimal AgreedSettlementAmount,
            string NegotiationRemarks)
        {
            if (User.IsInRole("Broker Company Officer"))
            {
                return Forbid();
            }

            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            if (!CanAccessClaim(claim))
            {
                return Forbid();
            }

            return await SaveNegotiationAsync(
                claim,
                claimId,
                NegotiationDate,
                ProposedSettlementAmount,
                AgreedSettlementAmount,
                NegotiationRemarks,
                SettlementStatus.Approved);
        }

        public async Task<IActionResult> OnPostRejectAsync(
            int claimId,
            string RejectionReason)
        {
            if (!CanRejectSettlement)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(RejectionReason))
            {
                TempData["ErrorMessage"] = "A rejection reason is required.";
                return RedirectToPage(new { claimId });
            }

            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            if (!CanAccessClaim(claim))
            {
                return Forbid();
            }

            claim.Status = ClaimStatus.Closed;
            claim.Notes = $"Settlement rejected: {RejectionReason}";

            var latestSettlement = claim.Settlements
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefault();

            if (latestSettlement == null)
            {
                latestSettlement = new ClaimSettlement
                {
                    InsuranceClaimId = claim.Id,
                    CreatedDate = DateTime.UtcNow
                };
                claim.Settlements.Add(latestSettlement);
            }

            latestSettlement.Status = SettlementStatus.Rejected;
            latestSettlement.Remarks = RejectionReason;
            latestSettlement.ModifiedDate = DateTime.UtcNow;

            await _claimService.UpdateClaimAsync(claim);

            StatusMessage = "Settlement has been rejected and the claim is now closed.";
            return RedirectToPage("/Claims/Index");
        }

        private async Task<IActionResult> SaveNegotiationAsync(
            InsuranceClaim claim,
            int claimId,
            DateTime negotiationDate,
            decimal proposedSettlementAmount,
            decimal agreedSettlementAmount,
            string negotiationRemarks,
            SettlementStatus negotiationStatus)
        {
            claim.Status = negotiationStatus == SettlementStatus.Approved
                ? ClaimStatus.DischargeVoucherPrepared
                : ClaimStatus.UnderNegotiation;
            claim.NegotiationStartDate = negotiationDate;
            claim.ApprovedAmount = proposedSettlementAmount;
            claim.SettledAmount = agreedSettlementAmount;
            claim.Notes = negotiationRemarks;

            var latestSettlement = claim.Settlements
                .OrderByDescending(settlement => settlement.CreatedDate)
                .FirstOrDefault();

            if (latestSettlement == null || latestSettlement.Status == SettlementStatus.Completed)
            {
                latestSettlement = new ClaimSettlement
                {
                    InsuranceClaimId = claim.Id,
                    CreatedDate = DateTime.UtcNow
                };

                claim.Settlements.Add(latestSettlement);
            }

            latestSettlement.ProposedAmount = proposedSettlementAmount;
            latestSettlement.ApprovedAmount = agreedSettlementAmount;
            latestSettlement.Status = negotiationStatus;
            latestSettlement.Remarks = negotiationRemarks;
            latestSettlement.ModifiedDate = DateTime.UtcNow;

            var userName = User.Identity?.Name;
            if (negotiationStatus == SettlementStatus.Approved)
            {
                latestSettlement.ApprovedBy = string.IsNullOrWhiteSpace(userName) ? latestSettlement.ApprovedBy : userName;
            }
            else
            {
                latestSettlement.ProposedBy = string.IsNullOrWhiteSpace(userName) ? latestSettlement.ProposedBy : userName;
            }

            await _claimService.UpdateClaimAsync(claim);

            if (negotiationStatus == SettlementStatus.Approved)
            {
                return RedirectToPage("/Claims/DischargeVoucher", new { claimId });
            }

            StatusMessage = negotiationStatus == SettlementStatus.Rejected
                ? "Negotiation saved with status: Settlement Rejected."
                : "Negotiation saved with status: Under Negotiation.";

            return RedirectToPage(new { claimId });
        }

        private bool CanAccessClaim(InsuranceClaim claim)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Insurance Officer"))
            {
                return true;
            }

            if (!User.IsInRole("Broker Company Officer"))
            {
                return false;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(userId) && claim.BrokerUserId == userId;
        }
    }
}
