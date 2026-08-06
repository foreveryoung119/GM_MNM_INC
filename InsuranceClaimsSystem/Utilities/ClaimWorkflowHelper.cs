using System.Security.Claims;
using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Utilities;

public static class ClaimWorkflowHelper
{
    public static IReadOnlyList<string> StageLabels { get; } =
    [
        "Claim Initiation",
        "Assessor Appointment",
        "Document Submission",
        "Negotiation on Settlement",
        "Discharge Voucher",
        "Proof of Payment",
        "Claim Closed"
    ];

    public static int GetCurrentStageNumber(ClaimStatus status)
    {
        return status switch
        {
            ClaimStatus.ClaimIntimation => 1,
            ClaimStatus.AssessorAppointed => 2,
            ClaimStatus.DocumentsRequested or ClaimStatus.DocumentsSubmitted => 3,
            ClaimStatus.UnderNegotiation => 4,
            ClaimStatus.DischargeVoucherPrepared => 5,
            ClaimStatus.PaymentReleased => 6,
            ClaimStatus.Closed or ClaimStatus.Rejected => 7,
            _ => 1
        };
    }

    public static string GetRouteForClaimStep(ClaimsPrincipal user, InsuranceClaim claim)
    {
        if (CanAccessBrokerDocumentFlow(user, claim) &&
            (claim.Status == ClaimStatus.ClaimIntimation || claim.Status == ClaimStatus.AssessorAppointed))
        {
            return $"/Claims/DocumentRequest/{claim.Id}";
        }

        if (CanAccessBrokerDocumentFlow(user, claim) && claim.Status == ClaimStatus.DocumentsRequested)
        {
            return $"/Claims/DocumentRequest/{claim.Id}";
        }

        if (CanAccessBrokerDocumentFlow(user, claim) && claim.Status == ClaimStatus.DocumentsSubmitted)
        {
            return $"/Claims/DocumentSubmission/{claim.Id}";
        }

        if (CanAccessAssessorFlow(user, claim) && claim.Status == ClaimStatus.ClaimIntimation)
        {
            return $"/Claims/AssessorAppointment/{claim.Id}";
        }

        if (CanAccessAssessorFlow(user, claim) && claim.Status == ClaimStatus.AssessorAppointed)
        {
            return $"/Claims/DocumentRequest/{claim.Id}";
        }

        if (CanAccessAssessorFlow(user, claim) &&
            (claim.Status == ClaimStatus.DocumentsRequested || claim.Status == ClaimStatus.DocumentsSubmitted))
        {
            return $"/Claims/DocumentSubmission/{claim.Id}";
        }

        if (CanAccessNegotiationFlow(user) && claim.Status == ClaimStatus.UnderNegotiation)
        {
            return $"/Claims/SettlementNegotiation/{claim.Id}";
        }

        if (CanAccessNegotiationFlow(user) && claim.Status == ClaimStatus.DischargeVoucherPrepared)
        {
            return $"/Claims/DischargeVoucher/{claim.Id}";
        }

        if (CanAccessPaymentFlow(user) && claim.Status == ClaimStatus.PaymentReleased)
        {
            return $"/Claims/PaymentPOP/{claim.Id}";
        }

        return $"/Claims/ClaimDetails/{claim.Id}";
    }

    private static bool CanAccessAssessorFlow(ClaimsPrincipal user, InsuranceClaim claim)
    {
        if (user.IsInRole("Admin"))
        {
            return true;
        }

        if (user.IsInRole("Insurance Officer"))
        {
            return true;
        }

        if (!user.IsInRole("Assessor"))
        {
            return false;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId) && claim.AssessedById == userId;
    }

    private static bool CanAccessNegotiationFlow(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || user.IsInRole("Insurance Officer") || user.IsInRole("Broker Company Officer");
    }

    private static bool CanAccessPaymentFlow(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || user.IsInRole("Lawyer");
    }

    private static bool CanAccessBrokerDocumentFlow(ClaimsPrincipal user, InsuranceClaim claim)
    {
        if (!user.IsInRole("Broker Company Officer"))
        {
            return false;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId) && claim.BrokerUserId == userId;
    }
}
