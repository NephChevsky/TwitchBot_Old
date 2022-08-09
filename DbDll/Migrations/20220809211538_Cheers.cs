using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class Cheers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Uptimes");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Messages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2022, 8, 9, 23, 15, 38, 386, DateTimeKind.Local).AddTicks(9675),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 5, 15, 14, 27, 661, DateTimeKind.Local).AddTicks(5201));

            migrationBuilder.CreateTable(
                name: "Cheers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Owner = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cheers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cheers_Id",
                table: "Cheers",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cheers");

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "Uptimes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2022, 8, 5, 15, 14, 27, 661, DateTimeKind.Local).AddTicks(5201),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 9, 23, 15, 38, 386, DateTimeKind.Local).AddTicks(9675));
        }
    }
}
