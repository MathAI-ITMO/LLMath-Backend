using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathLLMBackend.DataAccess.Migrations
{
    public partial class AddRolesAndMigrateUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { "1", "admin", "ADMIN", Guid.NewGuid().ToString() },
                    { "2", "user", "USER", Guid.NewGuid().ToString() }
                });

            migrationBuilder.Sql(@"
                INSERT INTO ""AspNetUserRoles"" (""UserId"", ""RoleId"")
                SELECT ""Id"", '2' FROM ""AspNetUsers""
                WHERE ""Id"" NOT IN (SELECT ""UserId"" FROM ""AspNetUserRoles"")
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"AspNetUserRoles\" WHERE \"RoleId\" IN ('1', '2')");
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1");
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2");
        }
    }
}
