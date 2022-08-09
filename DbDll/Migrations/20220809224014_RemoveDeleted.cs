using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class RemoveDeleted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Uptimes");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Messages");

            migrationBuilder.AddColumn<int>(
                name: "CheersCount",
                table: "Viewers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2022, 8, 10, 0, 40, 14, 277, DateTimeKind.Local).AddTicks(3381),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 5, 15, 14, 27, 661, DateTimeKind.Local).AddTicks(5201));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheersCount",
                table: "Viewers");

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
                oldDefaultValue: new DateTime(2022, 8, 10, 0, 40, 14, 277, DateTimeKind.Local).AddTicks(3381));
        }
    }
}
