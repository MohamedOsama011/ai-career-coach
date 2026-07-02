using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class update2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Invoiceid",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "referenceNumber",
                table: "Payments",
                newName: "intentkey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "intentkey",
                table: "Payments",
                newName: "referenceNumber");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceKey",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Invoiceid",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
