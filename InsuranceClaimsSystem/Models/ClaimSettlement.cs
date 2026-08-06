namespace InsuranceClaimsSystem.Models;

/// <summary>
/// Enumeration for settlement status.
/// </summary>
public enum SettlementStatus
{
    /// <summary>Settlement under negotiation.</summary>
    UnderNegotiation = 1,

    /// <summary>Settlement approved.</summary>
    Approved = 2,

    /// <summary>Settlement rejected.</summary>
    Rejected = 3,

    /// <summary>Settlement completed with payment.</summary>
    Completed = 4
}

/// <summary>
/// Represents a settlement record for an insurance claim.
/// </summary>
public class ClaimSettlement
{
    /// <summary>
    /// Gets or sets the unique identifier for the settlement.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the associated insurance claim.
    /// </summary>
    public int InsuranceClaimId { get; set; }

    /// <summary>
    /// Gets or sets the proposed settlement amount.
    /// </summary>
    public decimal ProposedAmount { get; set; }

    /// <summary>
    /// Gets or sets the final approved settlement amount.
    /// </summary>
    public decimal? ApprovedAmount { get; set; }

    /// <summary>
    /// Gets or sets the settlement status.
    /// </summary>
    public SettlementStatus Status { get; set; } = SettlementStatus.UnderNegotiation;

    /// <summary>
    /// Gets or sets the reason for denial if settlement was rejected.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Gets or sets the discharge voucher number.
    /// </summary>
    public string? DischargeVoucherNumber { get; set; }

    /// <summary>
    /// Gets or sets the DV prepared date.
    /// </summary>
    public DateTime? DVPreparedDate { get; set; }

    /// <summary>
    /// Gets or sets the proof of payment number.
    /// </summary>
    public string? ProofOfPaymentNumber { get; set; }

    /// <summary>
    /// Gets or sets the payment release date.
    /// </summary>
    public DateTime? PaymentReleasedDate { get; set; }

    /// <summary>
    /// Gets or sets the settlement proposed by.
    /// </summary>
    public string? ProposedBy { get; set; }

    /// <summary>
    /// Gets or sets the settlement approved by.
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Gets or sets remarks about the settlement.
    /// </summary>
    public string Remarks { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the settlement record was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the settlement record was last modified.
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Navigation property for the associated insurance claim.
    /// </summary>
    public virtual InsuranceClaim? InsuranceClaim { get; set; }
}
