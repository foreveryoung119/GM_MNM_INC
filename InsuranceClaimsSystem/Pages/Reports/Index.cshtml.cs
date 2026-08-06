using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;

namespace InsuranceClaimsSystem.Pages.Reports
{
    [Authorize(Roles = "Insurance Officer,Assessor,Broker Company Officer,Lawyer,Admin")]
    public class IndexModel : PageModel
    {
        private readonly IClaimService _claimService;

        public List<InsuranceClaim> Claims { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public ClaimStatus? Status { get; set; }

        public List<ClaimStatus> Statuses { get; } = Enum.GetValues<ClaimStatus>().ToList();

        public IndexModel(IClaimService claimService)
        {
            _claimService = claimService;
        }

        public async Task OnGetAsync()
        {
            Claims = await GetFilteredClaimsAsync();
        }

        public async Task<IActionResult> OnGetExportAsync(DateTime? fromDate, DateTime? toDate, ClaimStatus? status)
        {
            FromDate = fromDate;
            ToDate = toDate;
            Status = status;

            var claims = await GetFilteredClaimsAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Claims Report");

            sheet.Cell(1, 1).Value = "GM Sugar - Claims Report";
            sheet.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(14);

            sheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
            sheet.Cell(3, 1).Value = $"Filters: From={(FromDate?.ToString("yyyy-MM-dd") ?? "Any")}, To={(ToDate?.ToString("yyyy-MM-dd") ?? "Any")}, Status={(Status?.ToString() ?? "Any")}";

            var headers = new[]
            {
                "Claim Number", "Reported Person", "Claim Type", "Status", "Incident Date", "Created Date", "Estimated Loss", "Approved Amount", "Settled Amount"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                sheet.Cell(5, i + 1).Value = headers[i];
            }

            var headerRange = sheet.Range(5, 1, 5, headers.Length);
            headerRange.Style.Font.SetBold();
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 6;
            foreach (var claim in claims)
            {
                sheet.Cell(row, 1).Value = claim.ClaimNumber;
                sheet.Cell(row, 2).Value = claim.ReportedPersonName;
                sheet.Cell(row, 3).Value = string.IsNullOrWhiteSpace(claim.ClaimTypeOther) ? claim.ClaimType : $"{claim.ClaimType} - {claim.ClaimTypeOther}";
                sheet.Cell(row, 4).Value = claim.Status.ToString();
                sheet.Cell(row, 5).Value = claim.IncidentDate;
                sheet.Cell(row, 6).Value = claim.CreatedDate;
                sheet.Cell(row, 7).Value = claim.EstimatedLoss;
                sheet.Cell(row, 8).Value = claim.ApprovedAmount ?? 0m;
                sheet.Cell(row, 9).Value = claim.SettledAmount ?? 0m;

                sheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-mm-dd";
                sheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-mm-dd";
                sheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Claims_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private async Task<List<InsuranceClaim>> GetFilteredClaimsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            List<InsuranceClaim> baseClaims;

            if (User.IsInRole("Assessor") && !string.IsNullOrEmpty(userId))
            {
                baseClaims = await _claimService.GetAssessorClaimsAsync(userId);
            }
            else
            {
                baseClaims = await _claimService.GetAllClaimsAsync();

                if (User.IsInRole("Broker Company Officer") && !string.IsNullOrEmpty(userId))
                {
                    baseClaims = baseClaims.Where(c => c.BrokerUserId == userId).ToList();
                }
            }

            var query = baseClaims.AsEnumerable();

            if (FromDate.HasValue)
            {
                query = query.Where(c => c.CreatedDate.Date >= FromDate.Value.Date);
            }

            if (ToDate.HasValue)
            {
                query = query.Where(c => c.CreatedDate.Date <= ToDate.Value.Date);
            }

            if (Status.HasValue)
            {
                query = query.Where(c => c.Status == Status.Value);
            }

            return query
                .OrderByDescending(c => c.CreatedDate)
                .ToList();
        }
    }
}
