using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceClaimsSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBrokerUserToClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrokerUserId",
                table: "InsuranceClaims",
                type: "varchar(64)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_BrokerUserId",
                table: "InsuranceClaims",
                column: "BrokerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaims_AspNetUsers_BrokerUserId",
                table: "InsuranceClaims",
                column: "BrokerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaims_AspNetUsers_BrokerUserId",
                table: "InsuranceClaims");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceClaims_BrokerUserId",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "BrokerUserId",
                table: "InsuranceClaims");
        }
    }
}
