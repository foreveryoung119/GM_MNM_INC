using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;
using System.Security.Claims;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Insurance Officer,Admin")]
    public class ClaimIntimationModel : PageModel
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;

        [BindProperty]
        public InsuranceClaim Claim { get; set; } = new();

        public List<ApplicationUser> AvailableBrokers { get; set; } = new();

        public ClaimIntimationModel(IClaimService claimService, IUserService userService)
        {
            _claimService = claimService;
            _userService = userService;
        }

        public async Task OnGetAsync()
        {
            Claim.IntimationDate = DateTime.UtcNow;
            Claim.IncidentDate = DateTime.Today;
            AvailableBrokers = await _userService.GetAllBrokersAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Claim.ClaimType))
            {
                ModelState.AddModelError("Claim.ClaimType", "Claim Type is required.");
            }

            if (Claim.ClaimType == "Other" && string.IsNullOrWhiteSpace(Claim.ClaimTypeOther))
            {
                ModelState.AddModelError("Claim.ClaimTypeOther", "Please specify the other claim type.");
            }

            if (Claim.ClaimType != "Other")
            {
                Claim.ClaimTypeOther = null;
            }

            if (!ModelState.IsValid)
            {
                AvailableBrokers = await _userService.GetAllBrokersAsync();
                return Page();
            }

            Claim.Status = ClaimStatus.ClaimIntimation;
            Claim.IntimationDate = DateTime.UtcNow;
            Claim.CreatedDate = DateTime.UtcNow;
            Claim.CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _claimService.CreateClaimAsync(Claim);

            return RedirectToPage("/Claims/Index");
        }
    }
}
