using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTransactionInitiatorFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ApplicationUsers_InitiatorId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_InitiatorId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InitiatorId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InitiatedBy",
                table: "Transactions",
                column: "InitiatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ApplicationUsers_InitiatedBy",
                table: "Transactions",
                column: "InitiatedBy",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ApplicationUsers_InitiatedBy",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_InitiatedBy",
                table: "Transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "InitiatorId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InitiatorId",
                table: "Transactions",
                column: "InitiatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ApplicationUsers_InitiatorId",
                table: "Transactions",
                column: "InitiatorId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
