using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimsSystem.Services;
using InsuranceClaimsSystem.Models;
using System.Security.Claims;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Lawyer,Admin,Insurance Officer")]
    public class PaymentPOPModel : PageModel
    {
        private readonly IClaimService _claimService;
        private readonly IDocumentService _documentService;
        private readonly ILogger<PaymentPOPModel> _logger;
        private const string ProofOfPaymentDocumentDescription = "Proof of Payment";

        public InsuranceClaim? Claim { get; set; }

        public ClaimDocument? UploadedProofOfPayment { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public PaymentPOPModel(IClaimService claimService, IDocumentService documentService, ILogger<PaymentPOPModel> logger)
        {
            _claimService = claimService;
            _documentService = documentService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int claimId)
        {
            Claim = await _claimService.GetClaimByIdAsync(claimId);
            if (Claim == null)
            {
                return NotFound();
            }

            await LoadUploadedProofOfPaymentAsync(claimId);

            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(
            int claimId,
            IFormFile? POPFile)
        {
            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            if (POPFile == null || POPFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please choose a proof of payment file to upload.");
                Claim = claim;
                await LoadUploadedProofOfPaymentAsync(claimId);
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            try
            {
                await _documentService.UploadDocumentAsync(
                    claimId,
                    POPFile,
                    DocumentType.Other,
                    ProofOfPaymentDocumentDescription,
                    userId);

                StatusMessage = "Proof of payment uploaded successfully.";
                return RedirectToPage(new { claimId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading proof of payment for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, "Unable to upload proof of payment. Please try again.");
                Claim = claim;
                await LoadUploadedProofOfPaymentAsync(claimId);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(
            int claimId,
            DateTime PaymentDate,
            string? TransactionReference,
            IFormFile? POPFile,
            string? PaymentNotes)
        {
            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            await LoadUploadedProofOfPaymentAsync(claimId);

            if ((POPFile == null || POPFile.Length == 0) && UploadedProofOfPayment == null)
            {
                ModelState.AddModelError(string.Empty, "Please upload the proof of payment before completing the claim.");
            }

            if (!ModelState.IsValid)
            {
                Claim = claim;
                await LoadUploadedProofOfPaymentAsync(claimId);
                return Page();
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Forbid();
                }

                if (POPFile != null && POPFile.Length > 0)
                {
                    await _documentService.UploadDocumentAsync(
                        claimId,
                        POPFile,
                        DocumentType.Other,
                        ProofOfPaymentDocumentDescription,
                        userId);
                }

                claim.Status = ClaimStatus.Closed;
                claim.PaymentDate = PaymentDate;
                claim.PaymentReleasedDate = PaymentDate;
                claim.TransactionReference = TransactionReference?.Trim();
                claim.Notes = PaymentNotes?.Trim() ?? string.Empty;

                await _claimService.UpdateClaimAsync(claim);

                return RedirectToPage("/Claims/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while submitting proof of payment for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, "Unable to submit proof of payment. Please try again.");
                Claim = claim;
                await LoadUploadedProofOfPaymentAsync(claimId);
                return Page();
            }
        }

        public async Task<IActionResult> OnGetOpenAsync(int claimId, int documentId)
        {
            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            var document = await _documentService.GetDocumentByIdAsync(documentId);
            if (document == null || document.InsuranceClaimId != claimId)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(document.FilePath))
            {
                ErrorMessage = "The selected file could not be found on the server.";
                return RedirectToPage(new { claimId });
            }

            return PhysicalFile(document.FilePath, string.IsNullOrWhiteSpace(document.MimeType) ? "application/octet-stream" : document.MimeType);
        }

        private async Task LoadUploadedProofOfPaymentAsync(int claimId)
        {
            UploadedProofOfPayment = (await _documentService.GetClaimDocumentsAsync(claimId))
                .Where(document => document.DocumentType == DocumentType.Other)
                .Where(document => string.Equals(document.Description, ProofOfPaymentDocumentDescription, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(document => document.UploadedDate)
                .FirstOrDefault();
        }
    }
}
