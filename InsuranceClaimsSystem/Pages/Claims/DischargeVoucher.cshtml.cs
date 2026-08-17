using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;
using System.Security.Claims;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Insurance Officer,Broker Company Officer,Admin")]
    public class DischargeVoucherModel : PageModel
    {
        private readonly IClaimService _claimService;
        private readonly IDocumentService _documentService;
        private readonly ILogger<DischargeVoucherModel> _logger;
        private const string DischargeVoucherDocumentDescription = "Discharge Voucher";
        private const string DvNotAvailableToken = "[DVNA]";

        public InsuranceClaim? Claim { get; set; }

        public ClaimDocument? UploadedDischargeVoucher { get; set; }

        public bool IsDischargeVoucherNotAvailable { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public DischargeVoucherModel(IClaimService claimService, IDocumentService documentService, ILogger<DischargeVoucherModel> logger)
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

            if (!CanAccessClaim(Claim))
            {
                return Forbid();
            }

            await LoadUploadedDischargeVoucherAsync(claimId);
            IsDischargeVoucherNotAvailable = IsDvNotAvailable(Claim.Remarks);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            int claimId,
            string DVNumber,
            DateTime DVDate,
            decimal PaymentAmount,
            string PaymentMethod,
            string? BankDetails,
            string? DVNotes,
            IFormFile? DVFile)
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

            await LoadUploadedDischargeVoucherAsync(claimId);
            IsDischargeVoucherNotAvailable = IsDvNotAvailable(claim.Remarks);

            if ((DVFile == null || DVFile.Length == 0) && UploadedDischargeVoucher == null && !IsDischargeVoucherNotAvailable)
            {
                ModelState.AddModelError(string.Empty, "Please upload the discharge voucher before continuing to POP.");
            }

            if (!ModelState.IsValid)
            {
                Claim = claim;
                await LoadUploadedDischargeVoucherAsync(claimId);
                return Page();
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Forbid();
                }

                claim.Status = ClaimStatus.DischargeVoucherPrepared;
                claim.DVNumber = DVNumber?.Trim();
                claim.DischargeVoucherNumber = DVNumber?.Trim();
                claim.DischargeVoucherDate = DVDate;
                claim.SettledAmount = PaymentAmount;
                claim.PaymentMethod = PaymentMethod?.Trim();
                claim.AdditionalNotes = BankDetails?.Trim();
                claim.Notes = DVNotes?.Trim() ?? string.Empty;

                await _claimService.UpdateClaimAsync(claim);

                if (DVFile != null && DVFile.Length > 0)
                {
                    await _documentService.UploadDocumentAsync(
                        claimId,
                        DVFile,
                        DocumentType.Other,
                        DischargeVoucherDocumentDescription,
                        userId);

                    claim.Remarks = SetDvNotAvailable(claim.Remarks, false);
                    await _claimService.UpdateClaimAsync(claim);
                }

                StatusMessage = "Discharge voucher uploaded. Continue to POP.";

                return RedirectToPage("/Claims/PaymentPOP", new { claimId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while submitting discharge voucher for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, ex.Message);
                Claim = claim;
                await LoadUploadedDischargeVoucherAsync(claimId);
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while submitting discharge voucher for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, ex.Message);
                Claim = claim;
                await LoadUploadedDischargeVoucherAsync(claimId);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while submitting discharge voucher for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, "Unable to submit discharge voucher. Please verify inputs and try again.");
                Claim = claim;
                await LoadUploadedDischargeVoucherAsync(claimId);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUploadAsync(
            int claimId,
            IFormFile? DVFile)
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

            if (DVFile == null || DVFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please choose a discharge voucher file to upload.");
            }

            if (!ModelState.IsValid)
            {
                Claim = claim;
                await LoadUploadedDischargeVoucherAsync(claimId);
                return Page();
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Forbid();
                }

                await _documentService.UploadDocumentAsync(
                    claimId,
                    DVFile!,
                    DocumentType.Other,
                    DischargeVoucherDocumentDescription,
                    userId);

                claim.Remarks = SetDvNotAvailable(claim.Remarks, false);
                await _claimService.UpdateClaimAsync(claim);

                StatusMessage = "Discharge voucher uploaded successfully.";
                return RedirectToPage(new { claimId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while uploading discharge voucher for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, ex.Message);
                Claim = claim;
                await LoadUploadedDischargeVoucherAsync(claimId);
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while uploading discharge voucher for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, ex.Message);
                Claim = claim;
                await LoadUploadedDischargeVoucherAsync(claimId);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading discharge voucher for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, "Unable to upload discharge voucher. Please verify the file and try again.");
                Claim = claim;
                await LoadUploadedDischargeVoucherAsync(claimId);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSetDvNotAvailableAsync(int claimId, bool isNotAvailable)
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

            claim.Remarks = SetDvNotAvailable(claim.Remarks, isNotAvailable);
            await _claimService.UpdateClaimAsync(claim);

            StatusMessage = isNotAvailable
                ? "Discharge voucher marked as Not Available. You can continue to POP without a DV file."
                : "Discharge voucher file is required again unless already uploaded.";

            return RedirectToPage(new { claimId });
        }

        public async Task<IActionResult> OnGetOpenAsync(int claimId, int documentId)
        {
            var claim = await _claimService.GetClaimByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            if (!CanAccessClaim(claim))
            {
                return Forbid();
            }

            var document = await _documentService.GetDocumentByIdAsync(documentId);
            if (document == null || document.InsuranceClaimId != claimId)
            {
                return NotFound();
            }

            var resolvedPath = _documentService.ResolveDocumentPath(document);
            if (resolvedPath == null)
            {
                ErrorMessage = "The selected file could not be found on the server.";
                return RedirectToPage(new { claimId });
            }

            return PhysicalFile(resolvedPath, string.IsNullOrWhiteSpace(document.MimeType) ? "application/octet-stream" : document.MimeType);
        }

        private async Task LoadUploadedDischargeVoucherAsync(int claimId)
        {
            UploadedDischargeVoucher = (await _documentService.GetClaimDocumentsAsync(claimId))
                .Where(document => document.DocumentType == DocumentType.Other)
                .Where(document =>
                    string.Equals(document.Description, DischargeVoucherDocumentDescription, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(document.Description, "DV", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(document.Description, "Discharge Voucher (DV)", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(document => document.UploadedDate)
                .FirstOrDefault();
        }

        private static bool IsDvNotAvailable(string? remarks)
        {
            return !string.IsNullOrWhiteSpace(remarks) &&
                   remarks.Contains(DvNotAvailableToken, StringComparison.OrdinalIgnoreCase);
        }

        private static string SetDvNotAvailable(string? remarks, bool isNotAvailable)
        {
            var cleaned = (remarks ?? string.Empty)
                .Replace(DvNotAvailableToken, string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (!isNotAvailable)
            {
                return cleaned;
            }

            return string.IsNullOrWhiteSpace(cleaned)
                ? DvNotAvailableToken
                : $"{cleaned} {DvNotAvailableToken}";
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
