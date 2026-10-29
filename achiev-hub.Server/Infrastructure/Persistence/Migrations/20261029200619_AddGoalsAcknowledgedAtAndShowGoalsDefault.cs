using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achiev_hub.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalsAcknowledgedAtAndShowGoalsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "show_goals",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.Sql("""UPDATE "users" SET show_goals = false;""");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "goals_acknowledged_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "goals_acknowledged_at",
                table: "users");

            migrationBuilder.AlterColumn<bool>(
                name: "show_goals",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
