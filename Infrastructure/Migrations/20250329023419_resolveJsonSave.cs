using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class resolveJsonSave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "Transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(JObject),
                oldType: "jsonb");

            migrationBuilder.AddColumn<Guid>(
                name: "RefundWalletId",
                table: "Refunds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TransactionReceipt",
                table: "Refunds",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "WalletId",
                table: "CustomerPayouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayouts_WalletId",
                table: "CustomerPayouts",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPayouts_Wallets_WalletId",
                table: "CustomerPayouts",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPayouts_Wallets_WalletId",
                table: "CustomerPayouts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPayouts_WalletId",
                table: "CustomerPayouts");

            migrationBuilder.DropColumn(
                name: "RefundWalletId",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "TransactionReceipt",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "CustomerPayouts");

            migrationBuilder.AlterColumn<JObject>(
                name: "Details",
                table: "Transactions",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
