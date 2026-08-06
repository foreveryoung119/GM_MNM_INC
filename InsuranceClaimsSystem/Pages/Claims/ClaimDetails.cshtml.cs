using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Insurance Officer,Assessor,Broker Company Officer,Lawyer,Admin")]
    public class ClaimDetailsModel : PageModel
    {
        private readonly IClaimService _claimService;

        public InsuranceClaim? Claim { get; set; }

        public ClaimDetailsModel(IClaimService claimService)
        {
            _claimService = claimService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Claim = await _claimService.GetClaimByIdAsync(id);

            if (Claim == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Assessor"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || Claim.AssessedById != userId)
                {
                    return Forbid();
                }
            }

            if (User.IsInRole("Broker Company Officer"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || Claim.BrokerUserId != userId)
                {
                    return Forbid();
                }
            }

            return Page();
        }
    }
}
