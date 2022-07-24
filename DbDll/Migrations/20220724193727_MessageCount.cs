using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class MessageCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MessageCount",
                table: "Viewers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageCount",
                table: "Viewers");
        }
    }
}
