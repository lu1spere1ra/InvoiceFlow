using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHashChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "Invoices",
                type: "varchar(172)",
                unicode: false,
                maxLength: 172,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashControl",
                table: "Invoices",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastHash",
                table: "DocumentSeries",
                type: "varchar(172)",
                unicode: false,
                maxLength: 172,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hash",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "HashControl",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastHash",
                table: "DocumentSeries");
        }
    }
}
