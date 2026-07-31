using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDA.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 7, 25, 0, 0, 39, 812, DateTimeKind.Utc).AddTicks(8747), "$2a$11$E9rrbILGRkoJfkfVFps8W.K9X5vm/5e/CIfjuUST1TD/ATuaEkGea" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 7, 25, 0, 0, 31, 402, DateTimeKind.Utc).AddTicks(3027), "$2a$11$GLS2QlgKMOv/Jqmp87EOuuPHE.ZRofLJLtAKCt/8skMrtES4aK88G" });
        }
    }
}
