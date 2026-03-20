using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Main.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attachment_content_type",
                table: "messages",
                type: "varchar(512)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attachment_file_key",
                table: "messages",
                type: "varchar(512)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "attachment_file_size_in_bytes",
                table: "messages",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attachment_content_type",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "attachment_file_key",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "attachment_file_size_in_bytes",
                table: "messages");
        }
    }
}
