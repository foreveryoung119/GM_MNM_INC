using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceClaimsSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimTypeToInsuranceClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimType",
                table: "InsuranceClaims",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ClaimTypeOther",
                table: "InsuranceClaims",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimType",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "ClaimTypeOther",
                table: "InsuranceClaims");
        }
    }
}
