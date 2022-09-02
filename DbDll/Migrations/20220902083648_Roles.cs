using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class Roles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFollower",
                table: "Viewers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMod",
                table: "Viewers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSub",
                table: "Viewers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVIP",
                table: "Viewers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFollower",
                table: "Viewers");

            migrationBuilder.DropColumn(
                name: "IsMod",
                table: "Viewers");

            migrationBuilder.DropColumn(
                name: "IsSub",
                table: "Viewers");

            migrationBuilder.DropColumn(
                name: "IsVIP",
                table: "Viewers");
        }
    }
}
