using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IsCurrentBotUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "BotUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeStamp",
                table: "BotUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "BotUsers");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "BotUsers");
        }
    }
}
