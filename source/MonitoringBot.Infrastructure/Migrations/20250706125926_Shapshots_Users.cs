using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MonitoringBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Shapshots_Users : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_CurrentTimeSequenceNumber",
                table: "Events");

            migrationBuilder.AlterColumn<int>(
                name: "CurrentTimeSequenceNumber",
                table: "Events",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "BotUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IsForum = table.Column<bool>(type: "boolean", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateName = table.Column<string>(type: "text", nullable: false),
                    TotalEntities = table.Column<long>(type: "bigint", nullable: true),
                    LastProcessedEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastEventTimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastEventSequenceNumberForTimeStamp = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_TimeStamp_CurrentTimeSequenceNumber",
                table: "Events",
                columns: new[] { "TimeStamp", "CurrentTimeSequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_BotUsers_Id",
                table: "BotUsers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_LastEventTimeStamp",
                table: "Snapshots",
                column: "LastEventTimeStamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotUsers");

            migrationBuilder.DropTable(
                name: "Snapshots");

            migrationBuilder.DropIndex(
                name: "IX_Events_TimeStamp_CurrentTimeSequenceNumber",
                table: "Events");

            migrationBuilder.AlterColumn<long>(
                name: "CurrentTimeSequenceNumber",
                table: "Events",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CurrentTimeSequenceNumber",
                table: "Events",
                column: "CurrentTimeSequenceNumber");
        }
    }
}
