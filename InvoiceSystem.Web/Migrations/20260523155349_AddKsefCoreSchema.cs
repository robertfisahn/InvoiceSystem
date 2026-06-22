using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddKsefCoreSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KsefNumber",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KsefSentAt",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KsefTransactionId",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpoXml",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KsefIncomingInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KsefNumber = table.Column<string>(type: "TEXT", nullable: false),
                    SellerName = table.Column<string>(type: "TEXT", nullable: false),
                    SellerNip = table.Column<string>(type: "TEXT", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    RawXml = table.Column<string>(type: "TEXT", nullable: false),
                    ImportStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportedInvoiceId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KsefIncomingInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KsefSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nip = table.Column<string>(type: "TEXT", nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    ActiveSessionToken = table.Column<string>(type: "TEXT", nullable: true),
                    SessionExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KsefSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KsefIncomingInvoices_KsefNumber",
                table: "KsefIncomingInvoices",
                column: "KsefNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KsefIncomingInvoices");

            migrationBuilder.DropTable(
                name: "KsefSettings");

            migrationBuilder.DropColumn(
                name: "KsefNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "KsefSentAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "KsefTransactionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UpoXml",
                table: "Invoices");
        }
    }
}
