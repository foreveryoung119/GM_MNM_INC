namespace InsuranceClaimsSystem.Models;

/// <summary>
/// Enumeration for insurance claim status.
/// </summary>
public enum ClaimStatus
{
    /// <summary>Initial claim submission.</summary>
    ClaimIntimation = 1,

    /// <summary>Assessor has been appointed.</summary>
    AssessorAppointed = 2,

    /// <summary>Documents have been requested from the company.</summary>
    DocumentsRequested = 3,

    /// <summary>Documents have been submitted.</summary>
    DocumentsSubmitted = 4,

    /// <summary>Under negotiation for settlement amount.</summary>
    UnderNegotiation = 5,

    /// <summary>Discharge voucher has been prepared.</summary>
    DischargeVoucherPrepared = 6,

    /// <summary>Payment has been processed and released.</summary>
    PaymentReleased = 7,

    /// <summary>Claim has been rejected.</summary>
    Rejected = 8,

    /// <summary>Claim has been closed.</summary>
    Closed = 9,

    /// <summary>Claim has been cancelled by an administrator.</summary>
    Cancelled = 10
}

/// <summary>
/// Represents an insurance claim in the system.
/// </summary>
public class InsuranceClaim
{
    /// <summary>
    /// Gets or sets the unique identifier for the claim.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the claim number (e.g., CLM-2024-001).
    /// </summary>
    public string ClaimNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the claim.
    /// </summary>
    public ClaimStatus Status { get; set; } = ClaimStatus.ClaimIntimation;

    /// <summary>
    /// Gets or sets the date of claim intimation (loss/damage report).
    /// </summary>
    public DateTime IntimationDate { get; set; }

    /// <summary>
    /// Gets or sets the date of incident/loss.
    /// </summary>
    public DateTime IncidentDate { get; set; }

    /// <summary>
    /// Gets or sets the description of the loss or damage.
    /// </summary>
    public string IncidentDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the location where incident occurred.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated loss amount.
    /// </summary>
    public decimal EstimatedLoss { get; set; }

    /// <summary>
    /// Gets or sets the policy number.
    /// </summary>
    public string? PolicyNumber { get; set; }

    /// <summary>
    /// Gets or sets the claim type.
    /// </summary>
    public string ClaimType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the custom claim type when "Other" is selected.
    /// </summary>
    public string? ClaimTypeOther { get; set; }

    /// <summary>
    /// Gets or sets the contact number of the claimant.
    /// </summary>
    public string? ContactNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the person reporting the claim.
    /// </summary>
    public string ReportedPersonName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional notes about the claim.
    /// </summary>
    public string? AdditionalNotes { get; set; }

    /// <summary>
    /// Gets or sets the assessor's name.
    /// </summary>
    public string? AssessorName { get; set; }

    /// <summary>
    /// Gets or sets the assessor's email.
    /// </summary>
    public string? AssessorEmail { get; set; }

    /// <summary>
    /// Gets or sets the assessor's phone number.
    /// </summary>
    public string? AssessorPhone { get; set; }

    /// <summary>
    /// Gets or sets the assessor appointment date.
    /// </summary>
    public DateTime? AppointmentDate { get; set; }

    /// <summary>
    /// Gets or sets the approved amount.
    /// </summary>
    public decimal? ApprovedAmount { get; set; }

    /// <summary>
    /// Gets or sets the deductible amount.
    /// </summary>
    public decimal? DeductibleAmount { get; set; }

    /// <summary>
    /// Gets or sets the DV number.
    /// </summary>
    public string? DVNumber { get; set; }

    /// <summary>
    /// Gets or sets the payment method.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Gets or sets the payment date.
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Gets or sets the transaction reference.
    /// </summary>
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Gets or sets the notes or comments.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated claim amount.
    /// </summary>
    public decimal EstimatedAmount { get; set; }

    /// <summary>
    /// Gets or sets the settled claim amount.
    /// </summary>
    public decimal? SettledAmount { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this claim.
    /// </summary>
    public string? CreatedById { get; set; }

    /// <summary>
    /// Gets or sets the ID of the assigned assessor.
    /// </summary>
    public string? AssessedById { get; set; }

    /// <summary>
    /// Gets or sets the ID of the allocated broker user.
    /// </summary>
    public string? BrokerUserId { get; set; }

    /// <summary>
    /// Gets or sets the date when assessor was appointed.
    /// </summary>
    public DateTime? AssessorAppointedDate { get; set; }

    /// <summary>
    /// Gets or sets the date when documents were requested.
    /// </summary>
    public DateTime? DocumentRequestDate { get; set; }

    /// <summary>
    /// Gets or sets the date when documents were submitted.
    /// </summary>
    public DateTime? DocumentSubmissionDate { get; set; }

    /// <summary>
    /// Gets or sets the date when negotiation started.
    /// </summary>
    public DateTime? NegotiationStartDate { get; set; }

    /// <summary>
    /// Gets or sets the date when negotiation ended.
    /// </summary>
    public DateTime? NegotiationEndDate { get; set; }

    /// <summary>
    /// Gets or sets the date when discharge voucher was prepared.
    /// </summary>
    public DateTime? DischargeVoucherDate { get; set; }

    /// <summary>
    /// Gets or sets the DV reference number.
    /// </summary>
    public string? DischargeVoucherNumber { get; set; }

    /// <summary>
    /// Gets or sets the date when payment was released.
    /// </summary>
    public DateTime? PaymentReleasedDate { get; set; }

    /// <summary>
    /// Gets or sets the proof of payment number.
    /// </summary>
    public string? ProofOfPaymentNumber { get; set; }

    /// <summary>
    /// Gets or sets the remarks or comments on the claim.
    /// </summary>
    public string Remarks { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Gets or sets the user who last modified this claim.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Navigation property for the user who created this claim.
    /// </summary>
    public virtual ApplicationUser? CreatedBy { get; set; }

    /// <summary>
    /// Navigation property for the assessor assigned to this claim.
    /// </summary>
    public virtual ApplicationUser? AssessedBy { get; set; }

    /// <summary>
    /// Navigation property for the broker allocated to this claim.
    /// </summary>
    public virtual ApplicationUser? BrokerUser { get; set; }

    /// <summary>
    /// Navigation property for documents associated with this claim.
    /// </summary>
    public virtual ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();

    /// <summary>
    /// Navigation property for settlements associated with this claim.
    /// </summary>
    public virtual ICollection<ClaimSettlement> Settlements { get; set; } = new List<ClaimSettlement>();
}
