using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class MigrateId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey("PK_Viewers", "Viewers");

            migrationBuilder.DropIndex("IX_Viewers_Id", "Viewers");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Viewers",
                type: "nvarchar(512)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "Uptimes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.Sql("UPDATE Uptimes SET [dbo].[Uptimes].[Owner]=(SELECT [dbo].[Viewers].[TwitchId] FROM Viewers WHERE [dbo].[Viewers].[Id]=[dbo].[Uptimes].[Owner])");
            migrationBuilder.Sql("DELETE FROM Uptimes WHERE Owner is null");

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.Sql("UPDATE Messages SET [dbo].[Messages].[Owner]=(SELECT [dbo].[Viewers].[TwitchId] FROM Viewers WHERE [dbo].[Viewers].[Id]=[dbo].[Messages].[Owner])");
            migrationBuilder.Sql("DELETE FROM Messages WHERE Owner is null");

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "Commands",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.Sql("UPDATE Commands SET [dbo].[Commands].[Owner]=(SELECT [dbo].[Viewers].[TwitchId] FROM Viewers WHERE [dbo].[Viewers].[Id]=[dbo].[Commands].[Owner])");
            migrationBuilder.Sql("DELETE FROM Commands WHERE Owner is null");

            migrationBuilder.Sql("UPDATE Viewers SET Id=TwitchId");
            migrationBuilder.DropColumn("TwitchId", "Viewers");

            migrationBuilder.AddPrimaryKey("PK_Viewers", "Viewers", "Id");
            migrationBuilder.CreateIndex("IX_Viewers_Id", "Viewers", "Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsedDateTime",
                table: "ChannelRewards",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2022, 8, 10, 0, 51, 9, 395, DateTimeKind.Local).AddTicks(2947));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new Exception("Impossible to downgrade database: breaking changes");
        }
    }
}
