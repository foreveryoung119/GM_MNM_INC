using Microsoft.EntityFrameworkCore;
using InsuranceClaimsSystem.Data;
using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Services;

/// <summary>
/// Service for managing claim documents with secure file handling.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentService> _logger;
    private readonly string _uploadsDirectory;
    private readonly string _webRootPath;
    private readonly string _contentRootPath;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".xls" };

    private string AllowedExtensionsDisplay => string.Join(", ", AllowedExtensions);

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="webHostEnvironment">The web host environment.</param>
    public DocumentService(ApplicationDbContext context, ILogger<DocumentService> logger, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _logger = logger;
        _webRootPath = webHostEnvironment.WebRootPath;
        _contentRootPath = webHostEnvironment.ContentRootPath;
        _uploadsDirectory = Path.Combine(webHostEnvironment.WebRootPath, "uploads");

        if (!Directory.Exists(_uploadsDirectory))
        {
            Directory.CreateDirectory(_uploadsDirectory);
        }
    }

    /// <inheritdoc/>
    public async Task<ClaimDocument> UploadDocumentAsync(int claimId, IFormFile file, DocumentType documentType, string description, string uploadedBy)
    {
        try
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Please choose a file to upload.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException($"File is too large. Maximum allowed size is {MaxFileSizeBytes / (1024 * 1024)} MB.");

            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (!AllowedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Unsupported file type '{fileExtension}'. Allowed file types are: {AllowedExtensionsDisplay}.");

            // Verify claim exists
            var claim = await _context.InsuranceClaims.FindAsync(claimId);
            if (claim == null)
                throw new ArgumentException($"Claim with ID {claimId} not found");

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_uploadsDirectory, fileName);

            // Save file securely
            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Upload write access denied for path {FilePath}", filePath);
                throw new InvalidOperationException("Upload failed: the server could not write the file. Please contact support.");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Upload IO error for path {FilePath}", filePath);
                throw new InvalidOperationException("Upload failed due to a storage error. Please try again.");
            }

            // Create document record
            var document = new ClaimDocument
            {
                InsuranceClaimId = claimId,
                DocumentType = documentType,
                FileName = Path.GetFileNameWithoutExtension(file.FileName),
                FileExtension = fileExtension,
                FileSizeInBytes = file.Length,
                FilePath = filePath,
                MimeType = file.ContentType,
                Description = description,
                UploadedById = uploadedBy,
                UploadedDate = DateTime.UtcNow
            };

            _context.ClaimDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Document uploaded for claim {claimId}: {fileName}");
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading document: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ClaimDocument>> GetClaimDocumentsAsync(int claimId)
    {
        try
        {
            return await _context.ClaimDocuments
                .Where(d => d.InsuranceClaimId == claimId)
                .Include(d => d.UploadedBy)
                .Include(d => d.VerifiedBy)
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving documents for claim {claimId}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ClaimDocument?> GetDocumentByIdAsync(int documentId)
    {
        try
        {
            return await _context.ClaimDocuments
                .Include(d => d.InsuranceClaim)
                .Include(d => d.UploadedBy)
                .Include(d => d.VerifiedBy)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving document {documentId}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        try
        {
            var document = await _context.ClaimDocuments.FindAsync(documentId);
            if (document == null)
                return false;

            // Delete physical file
            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }

            // Delete database record
            _context.ClaimDocuments.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Document {documentId} deleted successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting document {documentId}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ClaimDocument> VerifyDocumentAsync(int documentId, string verifiedBy, string remarks)
    {
        try
        {
            var document = await _context.ClaimDocuments.FindAsync(documentId);
            if (document == null)
                throw new ArgumentException($"Document with ID {documentId} not found");

            document.IsVerified = true;
            document.VerifiedById = verifiedBy;
            document.VerifiedDate = DateTime.UtcNow;
            document.VerificationRemarks = remarks;

            _context.ClaimDocuments.Update(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Document {documentId} verified successfully");
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error verifying document: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<(byte[], string)> DownloadDocumentAsync(int documentId)
    {
        try
        {
            var document = await _context.ClaimDocuments.FindAsync(documentId);
            if (document == null)
                throw new ArgumentException($"Document with ID {documentId} not found");

            var resolvedPath = ResolveDocumentPath(document);
            if (resolvedPath == null)
                throw new InvalidOperationException("File not found on server");

            var fileBytes = await File.ReadAllBytesAsync(resolvedPath);
            var fileName = $"{document.FileName}{document.FileExtension}";

            _logger.LogInformation($"Document {documentId} downloaded");
            return (fileBytes, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error downloading document: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public string GetUploadsDirectory()
    {
        return _uploadsDirectory;
    }

    /// <inheritdoc/>
    public string? ResolveDocumentPath(ClaimDocument document)
    {
        if (File.Exists(document.FilePath))
        {
            return document.FilePath;
        }

        var candidates = new List<string>();

        var storedFileName = Path.GetFileName(document.FilePath);
        if (!string.IsNullOrWhiteSpace(storedFileName))
        {
            candidates.Add(Path.Combine(_uploadsDirectory, storedFileName));

            if (!string.IsNullOrWhiteSpace(_webRootPath))
            {
                candidates.Add(Path.Combine(_webRootPath, "uploads", storedFileName));
            }

            if (!string.IsNullOrWhiteSpace(_contentRootPath))
            {
                candidates.Add(Path.Combine(_contentRootPath, "uploads", storedFileName));
                candidates.Add(Path.Combine(_contentRootPath, "wwwroot", "uploads", storedFileName));
            }
        }

        if (!string.IsNullOrWhiteSpace(document.FilePath) && !Path.IsPathRooted(document.FilePath))
        {
            var relativePath = document.FilePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!string.IsNullOrWhiteSpace(_webRootPath))
            {
                candidates.Add(Path.Combine(_webRootPath, relativePath));
            }

            if (!string.IsNullOrWhiteSpace(_contentRootPath))
            {
                candidates.Add(Path.Combine(_contentRootPath, relativePath));
                candidates.Add(Path.Combine(_contentRootPath, "wwwroot", relativePath));
            }
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        if (!string.IsNullOrWhiteSpace(storedFileName) && Directory.Exists(_uploadsDirectory))
        {
            var firstMatch = Directory
                .EnumerateFiles(_uploadsDirectory, storedFileName, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstMatch))
            {
                return firstMatch;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public bool ValidateFileUpload(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxFileSizeBytes)
                return false;

            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (!AllowedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                return false;

            // Additional security checks can be added here
            // For example, scanning file content to prevent malware

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error validating file upload: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public long GetMaxFileSizeBytes()
    {
        return MaxFileSizeBytes;
    }
}
