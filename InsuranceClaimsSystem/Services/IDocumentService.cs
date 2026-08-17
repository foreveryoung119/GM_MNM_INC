using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Services;

/// <summary>
/// Service interface for managing claim documents.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Uploads a document for a claim.
    /// </summary>
    /// <param name="claimId">The claim ID.</param>
    /// <param name="file">The file to upload.</param>
    /// <param name="documentType">The type of document.</param>
    /// <param name="description">The document description.</param>
    /// <param name="uploadedBy">The user uploading the document.</param>
    /// <returns>The created claim document.</returns>
    Task<ClaimDocument> UploadDocumentAsync(int claimId, IFormFile file, DocumentType documentType, string description, string uploadedBy);

    /// <summary>
    /// Gets all documents for a claim.
    /// </summary>
    /// <param name="claimId">The claim ID.</param>
    /// <returns>List of documents for the claim.</returns>
    Task<List<ClaimDocument>> GetClaimDocumentsAsync(int claimId);

    /// <summary>
    /// Gets a specific document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>The document or null if not found.</returns>
    Task<ClaimDocument?> GetDocumentByIdAsync(int documentId);

    /// <summary>
    /// Deletes a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    Task<bool> DeleteDocumentAsync(int documentId);

    /// <summary>
    /// Verifies a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="verifiedBy">The user verifying the document.</param>
    /// <param name="remarks">The verification remarks.</param>
    /// <returns>The updated document.</returns>
    Task<ClaimDocument> VerifyDocumentAsync(int documentId, string verifiedBy, string remarks);

    /// <summary>
    /// Downloads a document file.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>Tuple of file bytes and file name.</returns>
    Task<(byte[], string)> DownloadDocumentAsync(int documentId);

    /// <summary>
    /// Resolves the current physical file path for a document.
    /// </summary>
    /// <param name="document">The document to resolve.</param>
    /// <returns>The resolved physical path, or null if the file cannot be found.</returns>
    string? ResolveDocumentPath(ClaimDocument document);

    /// <summary>
    /// Gets the uploads directory path.
    /// </summary>
    /// <returns>The uploads directory path.</returns>
    string GetUploadsDirectory();

    /// <summary>
    /// Validates file upload security (file type, size, etc).
    /// </summary>
    /// <param name="file">The file to validate.</param>
    /// <returns>True if file is valid, false otherwise.</returns>
    bool ValidateFileUpload(IFormFile file);

    /// <summary>
    /// Gets the maximum allowed file size in bytes.
    /// </summary>
    /// <returns>Maximum file size in bytes.</returns>
    long GetMaxFileSizeBytes();
}
