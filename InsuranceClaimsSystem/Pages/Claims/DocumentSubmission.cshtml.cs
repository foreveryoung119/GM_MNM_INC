using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimsSystem.Services;
using InsuranceClaimsSystem.Models;
using System.Security.Claims;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

namespace InsuranceClaimsSystem.Pages.Claims
{
    [Authorize(Roles = "Insurance Officer,Assessor,Broker Company Officer,Admin")]
    public class DocumentSubmissionModel : PageModel
    {
        private readonly IClaimService _claimService;
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentSubmissionModel> _logger;
        private const string RequestedDocsTokenPattern = @"\[REQDOCS:([^\]]*)\]";
        private const string RequestedDocsTextTokenPattern = @"\[REQDOCTEXT:([^\]]*)\]";
        private const string RequestedDocsStatusTokenPattern = @"\[REQDOCSTAT:([^\]]*)\]";
        private const string RequestedStatusSubmitted = "Submitted";
        private const string RequestedStatusNotSubmitted = "Not Submitted";
        private const string RequestedStatusNotAvailable = "Not Available";

        public DocumentSubmissionModel(IClaimService claimService, IDocumentService documentService, ILogger<DocumentSubmissionModel> logger)
        {
            _claimService = claimService;
            _documentService = documentService;
            _logger = logger;
        }

        public InsuranceClaim? Claim { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public List<RequestedUploadInput> RequestedUploads { get; set; } = new();

        [BindProperty]
        public string EstimatedLossInput { get; set; } = string.Empty;

        public List<ClaimDocument> ExistingDocuments { get; set; } = new();

        public List<DocumentType> RequiredDocumentTypes { get; set; } =
        [
            DocumentType.PoliceReport,
            DocumentType.Quotation,
            DocumentType.Photographs,
            DocumentType.Invoice,
            DocumentType.PolicyDocument,
            DocumentType.MedicalReport
        ];

        public HashSet<DocumentType> UploadedDocumentTypes { get; set; } = new();

        public List<string> RequestedDocumentNames { get; set; } = new();

        public List<string> RequestedSubmittedDocumentNames { get; set; } = new();

        public List<string> RequestedMissingDocumentNames { get; set; } = new();

        public List<RequestedDocumentStatusItem> RequestedDocumentStatuses { get; set; } = new();

        public Dictionary<string, int> RequestedLatestDocumentIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public bool UseRequestedDocumentWorkflow => Claim?.BrokerUserId != null || RequestedDocumentNames.Count > 0;

        public bool CanMarkNotAvailable => User.IsInRole("Admin") || User.IsInRole("Insurance Officer");

        public bool AllRequestedDocumentsResolved => RequestedDocumentStatuses.All(item => item.IsResolved);

        public List<DocumentType> MissingDocumentTypes => RequiredDocumentTypes
            .Where(type => !UploadedDocumentTypes.Contains(type))
            .ToList();

        public async Task<IActionResult> OnGetAsync(int claimId)
        {
            return await LoadPageAsync(claimId);
        }

        public async Task<IActionResult> OnPostAsync(
            int claimId,
            IFormFileCollection DocumentFile,
            DocumentType DocumentType,
            string SubmissionNotes)
        {
            try
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

                PopulateRequiredDocumentTypesFromClaim(claim);
                PopulateRequestedDocumentProgress();
                SetEstimatedLossInputFromClaim(claim);

                if (DocumentFile == null || DocumentFile.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Please upload at least one document.");
                    Claim = claim;
                    ExistingDocuments = await _documentService.GetClaimDocumentsAsync(claimId);
                    PopulateChecklistFromExistingDocuments();
                    PopulateRequestedDocumentProgress();
                    SetEstimatedLossInputFromClaim(claim);
                    return Page();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Forbid();
                }

                foreach (var file in DocumentFile)
                {
                    await _documentService.UploadDocumentAsync(claimId, file, DocumentType, SubmissionNotes ?? string.Empty, userId);
                }

                ExistingDocuments = await _documentService.GetClaimDocumentsAsync(claimId);
                PopulateChecklistFromExistingDocuments();

                claim.Status = ClaimStatus.DocumentsSubmitted;
                claim.DocumentSubmissionDate = DateTime.UtcNow;
                claim.Notes = SubmissionNotes ?? string.Empty;
                await _claimService.UpdateClaimAsync(claim);

                if (MissingDocumentTypes.Count == 0)
                {
                    StatusMessage = "All required documents are uploaded. Admin or Insurance Officer can proceed to negotiation.";
                }

                return RedirectToPage(new { claimId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while uploading documents for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, ex.Message);
                return await ReloadPageAsync(claimId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while uploading documents for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, ex.Message);
                return await ReloadPageAsync(claimId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while uploading documents for claim {ClaimId}", claimId);
                ModelState.AddModelError(string.Empty, "Unable to upload documents right now. Please try again.");
                return await ReloadPageAsync(claimId);
            }
        }

        public async Task<IActionResult> OnPostRequestedAsync(int claimId, string SubmissionNotes)
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

            Claim = claim;

            PopulateRequiredDocumentTypesFromClaim(claim);
            ExistingDocuments = await _documentService.GetClaimDocumentsAsync(claimId);
            PopulateChecklistFromExistingDocuments();
            PopulateRequestedDocumentProgress();
            SetEstimatedLossInputFromClaim(claim);

            if (!AllRequestedDocumentsResolved)
            {
                var unresolvedItems = RequestedDocumentStatuses
                    .Where(item => !item.IsResolved)
                    .Select(item => $"{item.DocumentName} ({item.Status})")
                    .ToList();

                ErrorMessage = unresolvedItems.Count > 0
                    ? $"Upload all requested documents or mark them Not Available before submitting. Pending: {string.Join(", ", unresolvedItems)}."
                    : "Upload all requested documents or mark them Not Available before submitting.";
                return RedirectToPage(new { claimId });
            }

            if (!TryApplyEstimatedLoss(claim, out var estimatedLossValidationMessage))
            {
                ErrorMessage = estimatedLossValidationMessage;
                return RedirectToPage(new { claimId });
            }

            var statusMap = GetRequestedDocumentStatusMap(claim.Remarks);
            foreach (var status in RequestedDocumentStatuses)
            {
                statusMap[status.DocumentName] = status.Status;
            }

            claim.Remarks = UpsertRequestedDocumentStatusToken(claim.Remarks, statusMap);
            claim.DocumentSubmissionDate = DateTime.UtcNow;
            claim.Notes = string.IsNullOrWhiteSpace(SubmissionNotes) ? claim.Notes : SubmissionNotes;

            if (AllRequestedDocumentsResolved && CanMarkNotAvailable)
            {
                claim.Status = ClaimStatus.UnderNegotiation;
                claim.NegotiationStartDate ??= DateTime.UtcNow;
                await _claimService.UpdateClaimAsync(claim);
                return RedirectToPage("/Claims/SettlementNegotiation", new { claimId });
            }

            claim.Status = ClaimStatus.DocumentsSubmitted;
            await _claimService.UpdateClaimAsync(claim);

            StatusMessage = "All requested documents are resolved. Admin or Insurance Officer can proceed to negotiation.";

            return RedirectToPage(new { claimId });
        }

        public async Task<IActionResult> OnPostUploadRequestedSingleAsync(int claimId, string documentName, IFormFile? file, string? SubmissionNotes)
        {
            try
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

                Claim = claim;

                var trimmedName = (documentName ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(trimmedName))
                {
                    ErrorMessage = "Missing requested document name.";
                    return RedirectToPage(new { claimId });
                }

                trimmedName = NormalizeRequestedDocumentName(trimmedName);

                if (file == null || file.Length == 0)
                {
                    ErrorMessage = $"Please choose a file for '{trimmedName}'.";
                    return RedirectToPage(new { claimId });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Forbid();
                }

                await _documentService.UploadDocumentAsync(
                    claimId,
                    file,
                    DocumentType.Other,
                    trimmedName,
                    userId);

                ExistingDocuments = await _documentService.GetClaimDocumentsAsync(claimId);
                PopulateRequiredDocumentTypesFromClaim(claim);
                PopulateChecklistFromExistingDocuments();
                PopulateRequestedDocumentProgress();

                var statusMap = GetRequestedDocumentStatusMap(claim.Remarks);
                foreach (var status in RequestedDocumentStatuses)
                {
                    statusMap[status.DocumentName] = status.Status;
                }

                claim.Remarks = UpsertRequestedDocumentStatusToken(claim.Remarks, statusMap);
                claim.DocumentSubmissionDate = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(SubmissionNotes))
                {
                    claim.Notes = SubmissionNotes;
                }

                claim.Status = ClaimStatus.DocumentsSubmitted;
                await _claimService.UpdateClaimAsync(claim);

                StatusMessage = $"Uploaded '{trimmedName}'.";
                return RedirectToPage(new { claimId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while uploading requested document for claim {ClaimId}", claimId);
                ErrorMessage = ex.Message;
                return RedirectToPage(new { claimId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while uploading requested document for claim {ClaimId}", claimId);
                ErrorMessage = ex.Message;
                return RedirectToPage(new { claimId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while uploading requested document for claim {ClaimId}", claimId);
                ErrorMessage = "Unable to upload the document right now. Please try again.";
                return RedirectToPage(new { claimId });
            }
        }

        public async Task<IActionResult> OnPostSetNotAvailableAsync(int claimId, string documentName, bool isNotAvailable)
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

            if (!CanMarkNotAvailable)
            {
                return Forbid();
            }

            Claim = claim;

            PopulateRequiredDocumentTypesFromClaim(claim);

            if (RequestedDocumentNames.Count == 0)
            {
                ErrorMessage = "No requested documents were found for this claim. Please go back to Document Request and add required documents.";
                return RedirectToPage(new { claimId });
            }

            var trimmedName = (documentName ?? string.Empty).Trim();
            trimmedName = NormalizeRequestedDocumentName(trimmedName);

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                ErrorMessage = "Missing requested document name.";
                return RedirectToPage(new { claimId });
            }

            ExistingDocuments = await _documentService.GetClaimDocumentsAsync(claimId);
            var uploadedNames = ExistingDocuments
                .Select(document => NormalizeRequestedDocumentName(document.Description))
                .Where(description => !string.IsNullOrWhiteSpace(description))
                .Select(description => description!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var statusMap = GetRequestedDocumentStatusMap(claim.Remarks);

            // Determine current status from server-side state and toggle deterministically.
            // This avoids relying on client-posted bool values that may be stale or inconsistent.
            statusMap.TryGetValue(trimmedName, out var mappedStatus);
            var normalizedMappedStatus = NormalizeRequestedStatus(mappedStatus);
            var currentStatus = uploadedNames.Contains(trimmedName)
                ? RequestedStatusSubmitted
                : (string.Equals(normalizedMappedStatus, RequestedStatusNotAvailable, StringComparison.OrdinalIgnoreCase)
                    ? RequestedStatusNotAvailable
                    : RequestedStatusNotSubmitted);

            var nextStatus = string.Equals(currentStatus, RequestedStatusNotAvailable, StringComparison.OrdinalIgnoreCase)
                ? (uploadedNames.Contains(trimmedName) ? RequestedStatusSubmitted : RequestedStatusNotSubmitted)
                : RequestedStatusNotAvailable;

            statusMap[trimmedName] = nextStatus;

            claim.Remarks = UpsertRequestedDocumentStatusToken(claim.Remarks, statusMap);

            PopulateRequestedDocumentProgress();

            claim.Status = ClaimStatus.DocumentsSubmitted;

            await _claimService.UpdateClaimAsync(claim);

            StatusMessage = string.Equals(nextStatus, RequestedStatusNotAvailable, StringComparison.OrdinalIgnoreCase)
                ? $"Marked '{trimmedName}' as Not Available."
                : $"Marked '{trimmedName}' as Not Submitted.";
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

            if (!System.IO.File.Exists(document.FilePath))
            {
                ErrorMessage = "The selected file could not be found on the server.";
                return RedirectToPage(new { claimId });
            }

            return PhysicalFile(document.FilePath, string.IsNullOrWhiteSpace(document.MimeType) ? "application/octet-stream" : document.MimeType);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int claimId, int documentId)
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

            var deleted = await _documentService.DeleteDocumentAsync(documentId);
            if (!deleted)
            {
                ErrorMessage = "Unable to delete the selected document.";
                return RedirectToPage(new { claimId });
            }

            StatusMessage = $"Deleted {document.FileName}{document.FileExtension}.";
            return RedirectToPage(new { claimId });
        }

        public async Task<IActionResult> OnPostReplaceAsync(int claimId, int documentId, IFormFile? replacementFile, string? replacementNotes)
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

            var existingDocument = await _documentService.GetDocumentByIdAsync(documentId);
            if (existingDocument == null || existingDocument.InsuranceClaimId != claimId)
            {
                return NotFound();
            }

            if (replacementFile == null || replacementFile.Length == 0)
            {
                ErrorMessage = "Choose a replacement file before submitting.";
                return RedirectToPage(new { claimId });
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
                    replacementFile,
                    existingDocument.DocumentType,
                    string.IsNullOrWhiteSpace(replacementNotes) ? existingDocument.Description : replacementNotes,
                    userId);

                await _documentService.DeleteDocumentAsync(documentId);
                StatusMessage = $"Replaced {existingDocument.FileName}{existingDocument.FileExtension}.";
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }

            return RedirectToPage(new { claimId });
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

        private void PopulateChecklistFromExistingDocuments()
        {
            UploadedDocumentTypes = ExistingDocuments
                .Select(d => d.DocumentType)
                .ToHashSet();
        }

        private void PopulateRequiredDocumentTypesFromClaim(InsuranceClaim claim)
        {
            RequestedDocumentNames = new List<string>();

            if (string.IsNullOrWhiteSpace(claim.Remarks))
            {
                return;
            }

            var textMatch = Regex.Match(claim.Remarks, RequestedDocsTextTokenPattern);
            if (textMatch.Success)
            {
                try
                {
                    var decodedBytes = Convert.FromBase64String(textMatch.Groups[1].Value);
                    var decoded = Encoding.UTF8.GetString(decodedBytes);

                    RequestedDocumentNames = decoded
                        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(Uri.UnescapeDataString)
                        .Select(NormalizeRequestedDocumentName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (FormatException)
                {
                    RequestedDocumentNames = new List<string>();
                }

                if (RequestedDocumentNames.Count > 0)
                {
                    RequiredDocumentTypes = [DocumentType.Other];
                    return;
                }
            }

            var match = Regex.Match(claim.Remarks, RequestedDocsTokenPattern);
            if (!match.Success)
            {
                return;
            }

            var selectedTypeIds = match.Groups[1].Value
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

            if (parsedTypes.Count > 0)
            {
                RequiredDocumentTypes = parsedTypes;
            }
        }

        private async Task<IActionResult> LoadPageAsync(int claimId)
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

            ExistingDocuments = await _documentService.GetClaimDocumentsAsync(claimId);
            PopulateRequiredDocumentTypesFromClaim(Claim);
            PopulateChecklistFromExistingDocuments();
            PopulateRequestedDocumentProgress();
            SetEstimatedLossInputFromClaim(Claim);

            return Page();
        }

        private void PopulateRequestedDocumentProgress()
        {
            if (RequestedDocumentNames.Count == 0)
            {
                RequestedSubmittedDocumentNames = new List<string>();
                RequestedMissingDocumentNames = new List<string>();
                RequestedDocumentStatuses = new List<RequestedDocumentStatusItem>();
                RequestedLatestDocumentIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var uploadedNameToLatestDocumentId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var submittedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var document in ExistingDocuments.OrderByDescending(d => d.UploadedDate))
            {
                foreach (var candidateName in GetCandidateDocumentNames(document))
                {
                    submittedNames.Add(candidateName);

                    if (!uploadedNameToLatestDocumentId.ContainsKey(candidateName))
                    {
                        uploadedNameToLatestDocumentId[candidateName] = document.Id;
                    }
                }
            }

            RequestedLatestDocumentIds = uploadedNameToLatestDocumentId;

            var normalizedRequestedNames = RequestedDocumentNames
                .Select(NormalizeRequestedDocumentName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var statusMap = GetRequestedDocumentStatusMap(Claim?.Remarks);

            RequestedDocumentStatuses = normalizedRequestedNames
                .Select(documentName =>
                {
                    var isSubmitted = submittedNames.Contains(documentName);
                    var status = RequestedStatusNotSubmitted;
                    statusMap.TryGetValue(documentName, out var mappedStatus);
                    var normalizedMappedStatus = NormalizeRequestedStatus(mappedStatus);

                    if (isSubmitted)
                    {
                        status = RequestedStatusSubmitted;
                    }
                    else if (string.Equals(normalizedMappedStatus, RequestedStatusSubmitted, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(normalizedMappedStatus, RequestedStatusNotAvailable, StringComparison.OrdinalIgnoreCase))
                    {
                        status = normalizedMappedStatus;
                    }

                    return new RequestedDocumentStatusItem
                    {
                        DocumentName = documentName,
                        Status = status
                    };
                })
                .ToList();

            RequestedSubmittedDocumentNames = RequestedDocumentStatuses
                .Where(item => string.Equals(item.Status, RequestedStatusSubmitted, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.DocumentName)
                .ToList();

            RequestedMissingDocumentNames = RequestedDocumentStatuses
                .Where(item => string.Equals(item.Status, RequestedStatusNotSubmitted, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.DocumentName)
                .ToList();
        }

        private static IEnumerable<string> GetCandidateDocumentNames(ClaimDocument document)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var normalizedDescription = NormalizeRequestedDocumentName(document.Description);
            if (!string.IsNullOrWhiteSpace(normalizedDescription))
            {
                names.Add(normalizedDescription);
            }

            var typeName = NormalizeRequestedDocumentName(GetDocumentTypeDisplayName(document.DocumentType));
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                names.Add(typeName);
            }

            // Add singular/plural compatibility for common requested names.
            if (string.Equals(typeName, "photographs", StringComparison.OrdinalIgnoreCase))
            {
                names.Add("photograph");
                names.Add("photo");
                names.Add("photos");
            }

            if (string.Equals(typeName, "invoice", StringComparison.OrdinalIgnoreCase))
            {
                names.Add("invoices");
            }

            return names;
        }

        private static string GetDocumentTypeDisplayName(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.Invoice => "Invoice",
                DocumentType.PoliceReport => "Police Report",
                DocumentType.Photographs => "Photographs",
                DocumentType.Quotation => "Quotation",
                DocumentType.MedicalReport => "Medical Report",
                DocumentType.PolicyDocument => "Policy Document",
                DocumentType.Other => "Other",
                _ => documentType.ToString()
            };
        }

        private static Dictionary<string, string> GetRequestedDocumentStatusMap(string? remarks)
        {
            var statusMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(remarks))
            {
                return statusMap;
            }

            var match = Regex.Match(remarks, RequestedDocsStatusTokenPattern);
            if (!match.Success)
            {
                return statusMap;
            }

            try
            {
                var decodedBytes = Convert.FromBase64String(match.Groups[1].Value);
                var decoded = Encoding.UTF8.GetString(decodedBytes);

                foreach (var entry in decoded.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    var documentName = NormalizeRequestedDocumentName(Uri.UnescapeDataString(parts[0]));
                    var status = NormalizeRequestedStatus(parts[1]);
                    if (string.IsNullOrWhiteSpace(documentName))
                    {
                        continue;
                    }

                    statusMap[documentName] = status;
                }
            }
            catch (FormatException)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return statusMap;
        }

        private static string UpsertRequestedDocumentStatusToken(string existingRemarks, Dictionary<string, string> statuses)
        {
            var normalizedStatuses = statuses
                .Select(entry => new KeyValuePair<string, string>(NormalizeRequestedDocumentName(entry.Key), NormalizeRequestedStatus(entry.Value)))
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

            var payload = string.Join("|", normalizedStatuses.Select(entry => $"{Uri.EscapeDataString(entry.Key)}={entry.Value}"));
            var token = $"[REQDOCSTAT:{Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))}]";

            if (string.IsNullOrWhiteSpace(existingRemarks))
            {
                return token;
            }

            var cleanedRemarks = Regex.Replace(existingRemarks, RequestedDocsStatusTokenPattern, string.Empty).Trim();
            return string.IsNullOrWhiteSpace(cleanedRemarks)
                ? token
                : $"{cleanedRemarks} {token}";
        }

        private static string NormalizeRequestedStatus(string? status)
        {
            if (string.Equals(status, RequestedStatusSubmitted, StringComparison.OrdinalIgnoreCase))
            {
                return RequestedStatusSubmitted;
            }

            if (string.Equals(status, RequestedStatusNotAvailable, StringComparison.OrdinalIgnoreCase))
            {
                return RequestedStatusNotAvailable;
            }

            return RequestedStatusNotSubmitted;
        }

        private static string NormalizeRequestedDocumentName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return Regex.Replace(name.Trim(), @"\s+", " ");
        }

        private async Task<IActionResult> ReloadPageAsync(int claimId)
        {
            Claim = await _claimService.GetClaimByIdAsync(claimId);
            if (Claim != null)
            {
                ExistingDocuments = await _documentService.GetClaimDocumentsAsync(claimId);
                PopulateRequiredDocumentTypesFromClaim(Claim);
                PopulateChecklistFromExistingDocuments();
                PopulateRequestedDocumentProgress();
                SetEstimatedLossInputFromClaim(Claim);
            }

            return Page();
        }

        private void SetEstimatedLossInputFromClaim(InsuranceClaim claim)
        {
            EstimatedLossInput = claim.EstimatedLoss > 0
                ? claim.EstimatedLoss.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private bool TryApplyEstimatedLoss(InsuranceClaim claim, out string? validationMessage)
        {
            validationMessage = null;

            var normalized = (EstimatedLossInput ?? string.Empty).Trim().Replace(",", string.Empty);
            if (string.IsNullOrWhiteSpace(normalized) ||
                !decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedEstimatedLoss) ||
                parsedEstimatedLoss <= 0)
            {
                validationMessage = "Estimated Loss Amount must be greater than zero before proceeding to Step 4.";
                return false;
            }

            claim.EstimatedLoss = parsedEstimatedLoss;
            return true;
        }

        public class RequestedUploadInput
        {
            public string DocumentName { get; set; } = string.Empty;

            public IFormFile? File { get; set; }
        }

        public class RequestedDocumentStatusItem
        {
            public string DocumentName { get; set; } = string.Empty;

            public string Status { get; set; } = RequestedStatusNotSubmitted;

            public bool IsResolved => string.Equals(Status, RequestedStatusSubmitted, StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(Status, RequestedStatusNotAvailable, StringComparison.OrdinalIgnoreCase);
        }
    }
}
