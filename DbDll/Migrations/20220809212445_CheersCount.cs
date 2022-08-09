using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class CheersCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                defaultValue: new DateTime(2022, 8, 9, 23, 24, 45, 806, DateTimeKind.Local).AddTicks(3914),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 9, 23, 15, 38, 386, DateTimeKind.Local).AddTicks(9675));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheersCount",
                table: "Viewers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2022, 8, 9, 23, 15, 38, 386, DateTimeKind.Local).AddTicks(9675),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 9, 23, 24, 45, 806, DateTimeKind.Local).AddTicks(3914));
        }
    }
}
