using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class betterindexing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Viewers_Username",
                table: "Viewers");

            migrationBuilder.DropIndex(
                name: "IX_Commands_Name",
                table: "Commands");

            migrationBuilder.CreateIndex(
                name: "IX_Viewers_Username_Deleted",
                table: "Viewers",
                columns: new[] { "Username", "Deleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commands_Name_Deleted",
                table: "Commands",
                columns: new[] { "Name", "Deleted" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Viewers_Username_Deleted",
                table: "Viewers");

            migrationBuilder.DropIndex(
                name: "IX_Commands_Name_Deleted",
                table: "Commands");

            migrationBuilder.CreateIndex(
                name: "IX_Viewers_Username",
                table: "Viewers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commands_Name",
                table: "Commands",
                column: "Name",
                unique: true);
        }
    }
}
