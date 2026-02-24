using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOldRefreshTokenTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "old_refresh_token_hash",
                table: "sessions",
                type: "varchar",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_refresh_token_key",
                table: "sessions",
                type: "varchar",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sessions_old_refresh_token_key",
                table: "sessions",
                column: "old_refresh_token_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sessions_old_refresh_token_key",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "old_refresh_token_hash",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "old_refresh_token_key",
                table: "sessions");
        }
    }
}
