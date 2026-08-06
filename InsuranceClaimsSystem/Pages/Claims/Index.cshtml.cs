using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;
using InsuranceClaimsSystem.Utilities;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Insurance Officer,Assessor,Broker Company Officer,Lawyer,Admin")]
    public class ClaimsIndexModel : PageModel
    {
        private readonly IClaimService _claimService;

        public List<InsuranceClaim> Claims { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public ClaimStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public List<ClaimStatus> StatusOptions { get; } = Enum.GetValues<ClaimStatus>().ToList();

        public ClaimsIndexModel(IClaimService claimService)
        {
            _claimService = claimService;
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<InsuranceClaim> baseClaims;

            if (User.IsInRole("Assessor") && !string.IsNullOrEmpty(userId))
            {
                baseClaims = await _claimService.GetAssessorClaimsAsync(userId);
            }
            else
            {
                var allClaims = await _claimService.GetAllClaimsAsync();

                if (User.IsInRole("Broker Company Officer") && !string.IsNullOrEmpty(userId))
                {
                    baseClaims = allClaims
                        .Where(c => c.BrokerUserId == userId)
                        .ToList();
                }
                else
                {
                    baseClaims = allClaims;
                }
            }

            var query = baseClaims.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                query = query.Where(c =>
                    (!string.IsNullOrWhiteSpace(c.ClaimNumber) && c.ClaimNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(c.IncidentDescription) && c.IncidentDescription.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(c.ReportedPersonName) && c.ReportedPersonName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(c.ClaimType) && c.ClaimType.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(c => c.Status == StatusFilter.Value);
            }

            if (FromDate.HasValue)
            {
                query = query.Where(c => c.CreatedDate.Date >= FromDate.Value.Date);
            }

            if (ToDate.HasValue)
            {
                query = query.Where(c => c.CreatedDate.Date <= ToDate.Value.Date);
            }

            Claims = query
                .OrderByDescending(c => c.CreatedDate)
                .ToList();
        }

        public async Task<IActionResult> OnGetOpenAsync(int claimId)
        {
            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Assessor") && (string.IsNullOrEmpty(userId) || claim.AssessedById != userId))
            {
                return Forbid();
            }

            if (User.IsInRole("Broker Company Officer") && (string.IsNullOrEmpty(userId) || claim.BrokerUserId != userId))
            {
                return Forbid();
            }

            var route = ClaimWorkflowHelper.GetRouteForClaimStep(User, claim);
            return LocalRedirect(route);
        }

        public async Task<IActionResult> OnPostCancelAsync(int claimId)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await _claimService.UpdateClaimStatusAsync(claimId, ClaimStatus.Cancelled, userId);

            return RedirectToPage();
        }
    }
}
