using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbDll.Migrations
{
    public partial class ChannelRewards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UserText = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BeginCost = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    CurrentCost = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    CostIncreaseAmount = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    CostDecreaseTimer = table.Column<int>(type: "int", nullable: false, defaultValue: 600),
                    SkipRewardQueue = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RedemptionCooldownTime = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    RedemptionPerStream = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RedemptionPerUserPerStream = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUsedDateTime = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "08/02/2022 00:49:26"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelRewards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelRewards_Id",
                table: "ChannelRewards",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelRewards");
        }
    }
}
