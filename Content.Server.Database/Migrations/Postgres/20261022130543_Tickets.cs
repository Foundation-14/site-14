using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Tickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ticket_auto_modifier",
                table: "player",
                type: "real",
                nullable: false,
                defaultValue: 1f);

            migrationBuilder.AddColumn<float>(
                name: "ticket_balance",
                table: "player",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "ticket_modifier_bonus",
                table: "player",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "ticket_paid_hours",
                table: "player",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ticket_auto_modifier",
                table: "player");

            migrationBuilder.DropColumn(
                name: "ticket_balance",
                table: "player");

            migrationBuilder.DropColumn(
                name: "ticket_modifier_bonus",
                table: "player");

            migrationBuilder.DropColumn(
                name: "ticket_paid_hours",
                table: "player");
        }
    }
}
