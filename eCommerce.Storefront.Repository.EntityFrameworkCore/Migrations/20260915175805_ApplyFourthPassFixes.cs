using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ApplyFourthPassFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId",
                table: "Customers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_UserId",
                table: "Customers");
        }
    }
}
