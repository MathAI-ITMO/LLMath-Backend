using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathLLMBackend.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProblems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProblemVariations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seed = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemVariations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProblemVariations_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProblemVariations_ProblemId",
                table: "ProblemVariations",
                column: "ProblemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProblemVariations");

            migrationBuilder.DropTable(
                name: "Problems");
        }
    }
}
