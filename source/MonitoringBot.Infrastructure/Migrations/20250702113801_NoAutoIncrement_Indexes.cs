using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MonitoringBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NoAutoIncrement_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SequenceNumber",
                table: "Events",
                newName: "CurrentTimeSequenceNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Events_SequenceNumber",
                table: "Events",
                newName: "IX_Events_CurrentTimeSequenceNumber");

            migrationBuilder.AlterColumn<long>(
                name: "CurrentTimeSequenceNumber",
                table: "Events",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_Id",
                table: "ChannelMembers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_LastAction",
                table: "ChannelMembers",
                column: "LastAction");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_TimeStamp",
                table: "ChannelMembers",
                column: "TimeStamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChannelMembers_Id",
                table: "ChannelMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChannelMembers_LastAction",
                table: "ChannelMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChannelMembers_TimeStamp",
                table: "ChannelMembers");

            migrationBuilder.RenameColumn(
                name: "CurrentTimeSequenceNumber",
                table: "Events",
                newName: "SequenceNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Events_CurrentTimeSequenceNumber",
                table: "Events",
                newName: "IX_Events_SequenceNumber");

            migrationBuilder.AlterColumn<long>(
                name: "SequenceNumber",
                table: "Events",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
