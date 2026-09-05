using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionCategoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the FK column, nullable for now so we can backfill it from the existing
            //    "Category" string column before enforcing NOT NULL.
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            // 2. Match each subscription's existing category name (already stored lowercased) to
            //    an existing Category row.
            migrationBuilder.Sql(
                """
                UPDATE "Subscriptions" s
                SET "CategoryId" = c."Id"
                FROM "Categories" c
                WHERE c."Name" = lower(trim(s."Category"));
                """);

            // 3. Safety net: this bug (renaming a category orphaned subscriptions that used the old
            //    name) may already have produced Subscriptions."Category" values with no matching
            //    Category row in production data. Rather than dropping that data, create a Category
            //    for each such orphaned name and attach the subscriptions to it.
            migrationBuilder.Sql(
                """
                INSERT INTO "Categories" ("Id", "Name")
                SELECT gen_random_uuid(), lower(trim(s."Category"))
                FROM "Subscriptions" s
                WHERE s."CategoryId" IS NULL
                GROUP BY lower(trim(s."Category"))
                ON CONFLICT ("Name") DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Subscriptions" s
                SET "CategoryId" = c."Id"
                FROM "Categories" c
                WHERE s."CategoryId" IS NULL AND c."Name" = lower(trim(s."Category"));
                """);

            // 4. Every row now has a CategoryId; enforce it.
            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Subscriptions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 5. Drop the old string column and its index.
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Category",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Subscriptions");

            // 6. Index + FK on the new column.
            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CategoryId",
                table: "Subscriptions",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Categories_CategoryId",
                table: "Subscriptions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Categories_CategoryId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CategoryId",
                table: "Subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Subscriptions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Subscriptions" s
                SET "Category" = c."Name"
                FROM "Categories" c
                WHERE s."CategoryId" = c."Id";
                """);

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Category",
                table: "Subscriptions",
                column: "Category");
        }
    }
}
