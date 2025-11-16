using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.src.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa các cột internal blockchain
            migrationBuilder.DropColumn(
                name: "PreviousHash",
                table: "BlockchainLedgers");

            migrationBuilder.DropColumn(
                name: "CurrentHash",
                table: "BlockchainLedgers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nếu rollback, thêm lại các cột internal blockchain
            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                table: "BlockchainLedgers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentHash",
                table: "BlockchainLedgers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
