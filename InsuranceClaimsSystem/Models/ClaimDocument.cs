namespace InsuranceClaimsSystem.Models;

/// <summary>
/// Enumeration for document types in insurance claims.
/// </summary>
public enum DocumentType
{
    /// <summary>Invoice or bill.</summary>
    Invoice = 1,

    /// <summary>Police report.</summary>
    PoliceReport = 2,

    /// <summary>Photographs or evidence.</summary>
    Photographs = 3,

    /// <summary>Repair quotation.</summary>
    Quotation = 4,

    /// <summary>Medical report.</summary>
    MedicalReport = 5,

    /// <summary>Insurance policy document.</summary>
    PolicyDocument = 6,

    /// <summary>Other supporting documents.</summary>
    Other = 7
}

/// <summary>
/// Represents a document uploaded for an insurance claim.
/// </summary>
public class ClaimDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for the document.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the associated insurance claim.
    /// </summary>
    public int InsuranceClaimId { get; set; }

    /// <summary>
    /// Gets or sets the document type.
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file extension (e.g., .pdf, .jpg).
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSizeInBytes { get; set; }

    /// <summary>
    /// Gets or sets the file path where the document is stored.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type of the document.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the document.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user who uploaded the document.
    /// </summary>
    public string UploadedById { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the document was uploaded.
    /// </summary>
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the document has been verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who verified the document.
    /// </summary>
    public string? VerifiedById { get; set; }

    /// <summary>
    /// Gets or sets when the document was verified.
    /// </summary>
    public DateTime? VerifiedDate { get; set; }

    /// <summary>
    /// Gets or sets the verification remarks.
    /// </summary>
    public string? VerificationRemarks { get; set; }

    /// <summary>
    /// Navigation property for the associated insurance claim.
    /// </summary>
    public virtual InsuranceClaim? InsuranceClaim { get; set; }

    /// <summary>
    /// Navigation property for the user who uploaded the document.
    /// </summary>
    public virtual ApplicationUser? UploadedBy { get; set; }

    /// <summary>
    /// Navigation property for the user who verified the document.
    /// </summary>
    public virtual ApplicationUser? VerifiedBy { get; set; }
}
