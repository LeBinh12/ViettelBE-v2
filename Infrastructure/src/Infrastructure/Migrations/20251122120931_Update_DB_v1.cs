using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.src.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_DB_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockchainLedgers_Invoices_InvoiceId1",
                table: "BlockchainLedgers");

            migrationBuilder.DropIndex(
                name: "IX_BlockchainLedgers_InvoiceId1",
                table: "BlockchainLedgers");

            migrationBuilder.DropColumn(
                name: "InvoiceId1",
                table: "BlockchainLedgers");

            migrationBuilder.AddColumn<DateTime>(
                name: "BlockchainRecordedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockchainTxHash",
                table: "Invoices",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockchainRecordedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BlockchainTxHash",
                table: "Invoices");

            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId1",
                table: "BlockchainLedgers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockchainLedgers_InvoiceId1",
                table: "BlockchainLedgers",
                column: "InvoiceId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockchainLedgers_Invoices_InvoiceId1",
                table: "BlockchainLedgers",
                column: "InvoiceId1",
                principalTable: "Invoices",
                principalColumn: "Id");
        }
    }
}
