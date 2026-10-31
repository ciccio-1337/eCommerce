using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "f01eb6e7-a59f-4094-a38e-db1acb888a27", "3d5bc7f9-e445-46fe-b284-894c3f0a1873" });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "3d5bc7f9-e445-46fe-b284-894c3f0a1873");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty: rolling this migration back must never restore
            // the published default administrator credential. Provision an admin
            // through the configured bootstrap instead.
        }
    }
}
