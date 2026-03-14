using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Main.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSearching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "messages",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('english', message_content)",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "title_search_vector",
                table: "chats",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('english', title)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_search_vector",
                table: "messages",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_chats_title_search_vector",
                table: "chats",
                column: "title_search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_search_vector",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "ix_chats_title_search_vector",
                table: "chats");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "title_search_vector",
                table: "chats");
        }
    }
}
