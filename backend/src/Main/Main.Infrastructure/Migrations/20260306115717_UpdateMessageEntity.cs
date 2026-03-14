using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Main.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token_count",
                table: "messages",
                newName: "total_token_count");

            migrationBuilder.AddColumn<long>(
                name: "input_token_count",
                table: "messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "output_token_count",
                table: "messages",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "input_token_count",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "output_token_count",
                table: "messages");

            migrationBuilder.RenameColumn(
                name: "total_token_count",
                table: "messages",
                newName: "token_count");
        }
    }
}
