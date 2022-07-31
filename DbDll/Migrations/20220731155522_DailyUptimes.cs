using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class DailyUptimes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Uptimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sum = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Owner = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uptimes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Uptimes_Id",
                table: "Uptimes",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Uptimes");
        }
    }
}
