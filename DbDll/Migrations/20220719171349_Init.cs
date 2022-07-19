using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Viewers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TwitchId = table.Column<int>(type: "int", maxLength: 512, nullable: false),
                    IsBot = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Seen = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastViewedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Uptime = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Viewers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Viewers_Id",
                table: "Viewers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viewers_Username",
                table: "Viewers",
                column: "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Viewers");
        }
    }
}
