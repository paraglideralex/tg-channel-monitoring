using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Snapshots_TimeStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TimeStamp",
                table: "Snapshots",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "Snapshots");
        }
    }
}
