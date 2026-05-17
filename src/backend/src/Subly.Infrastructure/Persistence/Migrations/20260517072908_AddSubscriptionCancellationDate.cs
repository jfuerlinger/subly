using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionCancellationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CancelledAt",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CancelledAt",
                table: "Subscriptions",
                column: "CancelledAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CancelledAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Subscriptions");
        }
    }
}
