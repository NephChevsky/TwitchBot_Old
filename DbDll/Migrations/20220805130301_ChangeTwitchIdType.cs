using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class ChangeTwitchIdType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TwitchId",
                table: "ChannelRewards",
                type: "uniqueidentifier",
                maxLength: 512,
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2022, 8, 5, 15, 3, 1, 787, DateTimeKind.Local).AddTicks(1825),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 4, 22, 43, 48, 384, DateTimeKind.Local).AddTicks(536));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TwitchId",
                table: "ChannelRewards",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2022, 8, 4, 22, 43, 48, 384, DateTimeKind.Local).AddTicks(536),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 5, 15, 3, 1, 787, DateTimeKind.Local).AddTicks(1825));
        }
    }
}
