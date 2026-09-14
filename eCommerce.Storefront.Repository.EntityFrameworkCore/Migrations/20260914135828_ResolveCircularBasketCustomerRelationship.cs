using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ResolveCircularBasketCustomerRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Baskets_Customers_CustomerId",
                table: "Baskets");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Baskets_BasketId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_BasketId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Baskets_CustomerId",
                table: "Baskets");

            // UpdateData (not DeleteData/InsertData): the baseline migration seeded the Admin role
            // against a placeholder user id that no longer exists. Updating is idempotent — it is a
            // no-op on databases where the corrected row already exists, avoiding a UNIQUE conflict on
            // AspNetUserRoles(UserId, RoleId), while still fixing the role binding on clean databases.
            migrationBuilder.UpdateData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "f01eb6e7-a59f-4094-a38e-db1acb888a27", "c8dd25ca-5d15-4a39-8023-7199a7f84286" },
                columns: new[] { "UserId" },
                values: new object[] { "3d5bc7f9-e445-46fe-b284-894c3f0a1873" });

            migrationBuilder.DropColumn(
                name: "BasketId",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Baskets_CustomerId",
                table: "Baskets",
                column: "CustomerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Baskets_Customers_CustomerId",
                table: "Baskets",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Baskets_Customers_CustomerId",
                table: "Baskets");

            migrationBuilder.DropIndex(
                name: "IX_Baskets_CustomerId",
                table: "Baskets");

            migrationBuilder.UpdateData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "f01eb6e7-a59f-4094-a38e-db1acb888a27", "3d5bc7f9-e445-46fe-b284-894c3f0a1873" },
                columns: new[] { "UserId" },
                values: new object[] { "c8dd25ca-5d15-4a39-8023-7199a7f84286" });

            migrationBuilder.AddColumn<Guid>(
                name: "BasketId",
                table: "Customers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BasketId",
                table: "Customers",
                column: "BasketId");

            migrationBuilder.CreateIndex(
                name: "IX_Baskets_CustomerId",
                table: "Baskets",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Baskets_Customers_CustomerId",
                table: "Baskets",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Baskets_BasketId",
                table: "Customers",
                column: "BasketId",
                principalTable: "Baskets",
                principalColumn: "BasketId");
        }
    }
}
