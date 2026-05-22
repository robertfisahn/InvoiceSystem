using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class ChangeInvoiceItemQuantityToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "InvoiceItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InvoiceItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
