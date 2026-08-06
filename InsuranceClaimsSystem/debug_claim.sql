-- Check claim CLM-2026-0003 status
SELECT 
    Id,
    ClaimNumber,
    Status,
    BrokerUserId,
    Notes,
    Remarks
FROM 
    insurance_claims
WHERE 
    ClaimNumber = 'CLM-2026-0003'
LIMIT 1;

-- Check if there are any uploaded documents for this claim
SELECT 
    Id,
    ClaimId,
    FileName,
    FileExtension,
    Description,
    DocumentType,
    UploadedDate
FROM 
    claim_documents
WHERE 
    ClaimId = (SELECT Id FROM insurance_claims WHERE ClaimNumber = 'CLM-2026-0003' LIMIT 1)
ORDER BY 
    UploadedDate DESC;
