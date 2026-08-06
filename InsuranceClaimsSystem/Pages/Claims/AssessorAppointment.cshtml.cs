using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Insurance Officer,Broker Company Officer,Admin")]
    public class AssessorAppointmentModel : PageModel
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;

        public InsuranceClaim? Claim { get; set; }
        public List<ApplicationUser> AvailableAssessors { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public AssessorAppointmentModel(IClaimService claimService, IUserService userService)
        {
            _claimService = claimService;
            _userService = userService;
        }

        public async Task<IActionResult> OnGetAsync(int claimId)
        {
            await LoadPageDataAsync(claimId);

            Claim = await _claimService.GetClaimByIdAsync(claimId);
            if (Claim == null)
            {
                return NotFound();
            }

            if (!CanAccessClaim(Claim))
            {
                return Forbid();
            }

            // Pre-populate from existing claim data if an assessor was previously assigned
            if (!string.IsNullOrEmpty(Claim.AssessedById))
            {
                Input.SelectedAssessorId = Claim.AssessedById;
            }

            if (!string.IsNullOrEmpty(Claim.Notes))
            {
                Input.AppointmentNotes = Claim.Notes;
            }

            Input.AppointmentDate = Claim.AppointmentDate.HasValue && Claim.AppointmentDate.Value != default
                ? Claim.AppointmentDate.Value
                : DateTime.Today;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int claimId)
        {
            if (User.IsInRole("Broker Company Officer"))
            {
                return Forbid();
            }

            await LoadPageDataAsync(claimId);

            if (!ModelState.IsValid)
            {
                return Page();
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

            var assessor = await _userService.GetUserByIdAsync(Input.SelectedAssessorId);
            if (assessor == null)
            {
                ModelState.AddModelError(nameof(Input.SelectedAssessorId), "Selected assessor was not found.");
                return Page();
            }

            claim.Status = ClaimStatus.AssessorAppointed;
            claim.AssessedById = assessor.Id;
            claim.AssessorName = assessor.FullName;
            claim.AssessorEmail = assessor.Email;
            claim.AssessorPhone = assessor.PhoneNumber;
            claim.AppointmentDate = Input.AppointmentDate;
            claim.AssessorAppointedDate = DateTime.UtcNow;
            claim.Notes = Input.AppointmentNotes ?? string.Empty;

            await _claimService.UpdateClaimAsync(claim);

            return RedirectToPage("/Claims/DocumentRequest", new { claimId });
        }

        public async Task<IActionResult> OnPostSkipAsync(int claimId)
        {
            ModelState.Clear();

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

            claim.Status = ClaimStatus.DocumentsRequested;
            claim.DocumentRequestDate = DateTime.UtcNow;

            await _claimService.UpdateClaimAsync(claim);

            return RedirectToPage("/Claims/DocumentRequest", new { claimId });
        }

        private async Task LoadPageDataAsync(int claimId)
        {
            Claim = await _claimService.GetClaimByIdAsync(claimId);
            AvailableAssessors = await _userService.GetAllAssessorsAsync();
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

        public class InputModel
        {
            [Required]
            [Display(Name = "Assessor")]
            public string SelectedAssessorId { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Appointment Date")]
            public DateTime AppointmentDate { get; set; }

            [Display(Name = "Appointment Notes")]
            public string? AppointmentNotes { get; set; }
        }
    }
}
