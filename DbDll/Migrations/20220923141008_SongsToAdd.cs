using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class SongsToAdd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SongsToAdd",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", maxLength: 512, nullable: false),
                    Uri = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    RewardId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EventId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongsToAdd", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongsToAdd_Id",
                table: "SongsToAdd",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SongsToAdd");
        }
    }
}
