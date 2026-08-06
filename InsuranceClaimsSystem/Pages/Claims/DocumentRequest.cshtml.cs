using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.Text;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Insurance Officer,Assessor,Broker Company Officer,Admin")]
    public class DocumentRequestModel : PageModel
    {
        private readonly IClaimService _claimService;
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentRequestModel> _logger;
        private const string RequestedDocsTokenPattern = @"\[REQDOCS:[^\]]*\]";
        private const string RequestedDocsTextTokenPattern = @"\[REQDOCTEXT:([^\]]*)\]";

        public InsuranceClaim? Claim { get; set; }
        public List<string> RequestedDocumentNames { get; set; } = new();

        public DocumentRequestModel(IClaimService claimService, IDocumentService documentService, ILogger<DocumentRequestModel> logger)
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

            RequestedDocumentNames = await GetRequestedDocumentNamesAsync(Claim.Id, Claim.Remarks);
            if (RequestedDocumentNames.Count == 0)
            {
                RequestedDocumentNames.Add(string.Empty);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            int claimId,
            List<string>? RequestedDocuments,
            string? RequestNotes)
        {
            if (claimId <= 0)
            {
                return BadRequest();
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

            var requestedDocNames = (RequestedDocuments ?? new List<string>())
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            RequestedDocumentNames = RequestedDocuments?.Count > 0
                ? RequestedDocuments
                    .Select(d => string.IsNullOrWhiteSpace(d) ? string.Empty : d.Trim())
                    .ToList()
                : new List<string> { string.Empty };

            if (requestedDocNames.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please add at least one required document.");
                Claim = claim;
                RequestedDocumentNames = RequestedDocumentNames.Count > 0 ? RequestedDocumentNames : new List<string> { string.Empty };
                return Page();
            }

            try
            {
                // Use submitted documents as the complete list (form already has existing docs pre-filled)
                // This way, deleted documents are removed from requirements
                claim.Status = ClaimStatus.DocumentsRequested;
                claim.DocumentRequestDate = DateTime.UtcNow;
                claim.Notes = RequestNotes ?? string.Empty;
                claim.Remarks = UpsertRequestedDocsTextToken(claim.Remarks, requestedDocNames);

                await _claimService.UpdateClaimAsync(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send document request for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, "Unable to send document request right now. Please try again.");
                Claim = claim;
                return Page();
            }

            return RedirectToPage("/Claims/DocumentSubmission", new { claimId });
        }

        private bool CanAccessClaim(InsuranceClaim claim)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Insurance Officer"))
            {
                return true;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            if (User.IsInRole("Assessor"))
            {
                return claim.AssessedById == userId;
            }

            if (User.IsInRole("Broker Company Officer"))
            {
                return claim.BrokerUserId == userId;
            }

            return false;
        }

        private static string UpsertRequestedDocsTextToken(string existingRemarks, List<string> selectedDocuments)
        {
            var joined = string.Join("|", selectedDocuments.Select(Uri.EscapeDataString));
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(joined));
            var token = $"[REQDOCTEXT:{payload}]";

            if (string.IsNullOrWhiteSpace(existingRemarks))
            {
                return token;
            }

            var cleanedRemarks = Regex.Replace(existingRemarks, RequestedDocsTokenPattern, string.Empty);
            cleanedRemarks = Regex.Replace(cleanedRemarks, RequestedDocsTextTokenPattern, string.Empty).Trim();
            return string.IsNullOrWhiteSpace(cleanedRemarks)
                ? token
                : $"{cleanedRemarks} {token}";
        }

        private async Task<List<string>> GetRequestedDocumentNamesAsync(int claimId, string? remarks)
        {
            var requestedDocuments = GetRequestedDocumentNamesFromRemarks(remarks);
            if (requestedDocuments.Count > 0)
            {
                return requestedDocuments;
            }

            var uploadedRequestedNames = (await _documentService.GetClaimDocumentsAsync(claimId))
                .Where(document => document.DocumentType == DocumentType.Other && !string.IsNullOrWhiteSpace(document.Description))
                .Select(document => document.Description.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return uploadedRequestedNames;
        }

        private static List<string> GetRequestedDocumentNamesFromRemarks(string? remarks)
        {
            var requestedDocuments = new List<string>();

            if (string.IsNullOrWhiteSpace(remarks))
            {
                return requestedDocuments;
            }

            var textMatch = Regex.Match(remarks, RequestedDocsTextTokenPattern);
            if (textMatch.Success)
            {
                try
                {
                    var decodedBytes = Convert.FromBase64String(textMatch.Groups[1].Value);
                    var decoded = Encoding.UTF8.GetString(decodedBytes);

                    requestedDocuments = decoded
                        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(Uri.UnescapeDataString)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (FormatException)
                {
                    requestedDocuments = new List<string>();
                }

                if (requestedDocuments.Count > 0)
                {
                    return requestedDocuments;
                }
            }

            var legacyMatch = Regex.Match(remarks, RequestedDocsTokenPattern);
            if (!legacyMatch.Success)
            {
                return requestedDocuments;
            }

            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Police Report"] = "Police Report",
                ["Quotation"] = "Repair Quotation",
                ["Photos"] = "Photographs",
                ["Purchase Invoice"] = "Purchase Invoice",
                ["Insurance Policy"] = "Insurance Policy",
                ["Bank Details"] = "Bank Details"
            };

            var selectedTypeIds = legacyMatch.Groups[1].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var parsedTypes = selectedTypeIds
                .Where(id => Enum.IsDefined(typeof(DocumentType), id))
                .Select(id => (DocumentType)id)
                .ToList();

            foreach (var type in parsedTypes)
            {
                var name = type switch
                {
                    DocumentType.PoliceReport => "Police Report",
                    DocumentType.Quotation => "Repair Quotation",
                    DocumentType.Photographs => "Photographs",
                    DocumentType.Invoice => "Purchase Invoice",
                    DocumentType.PolicyDocument => "Insurance Policy",
                    DocumentType.MedicalReport => "Medical Report",
                    _ => "Other"
                };

                requestedDocuments.Add(name);
            }

            return requestedDocuments.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
