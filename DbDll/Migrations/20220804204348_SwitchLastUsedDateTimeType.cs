using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class SwitchLastUsedDateTimeType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2022, 8, 4, 22, 43, 48, 384, DateTimeKind.Local).AddTicks(536),
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "08/04/2022 22:34:47");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "08/04/2022 22:34:47",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 4, 22, 43, 48, 384, DateTimeKind.Local).AddTicks(536));
        }
    }
}
