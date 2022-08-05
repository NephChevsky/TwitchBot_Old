using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class ChannelReward_TwitchId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "08/04/2022 22:34:47",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "08/02/2022 18:52:15");

            migrationBuilder.AddColumn<string>(
                name: "TwitchId",
                table: "ChannelRewards",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwitchId",
                table: "ChannelRewards");

            migrationBuilder.AlterColumn<string>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "08/02/2022 18:52:15",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "08/04/2022 22:34:47");
        }
    }
}
