using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class ChannelRewardBackgroundColor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "08/02/2022 18:52:15",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "08/02/2022 18:50:27");

            migrationBuilder.AddColumn<string>(
                name: "BackgroundColor",
                table: "ChannelRewards",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#ffffff");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundColor",
                table: "ChannelRewards");

            migrationBuilder.AlterColumn<string>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "08/02/2022 18:50:27",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "08/02/2022 18:52:15");
        }
    }
}
