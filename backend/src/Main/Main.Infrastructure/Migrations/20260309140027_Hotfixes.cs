using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Main.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Hotfixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "workflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_chats_folder_id",
                table: "chats",
                column: "folder_id");

            migrationBuilder.AddForeignKey(
                name: "fk_chats_folders_folder_id",
                table: "chats",
                column: "folder_id",
                principalTable: "folders",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_chats_folders_folder_id",
                table: "chats");

            migrationBuilder.DropIndex(
                name: "ix_chats_folder_id",
                table: "chats");

            migrationBuilder.DropColumn(
                name: "version",
                table: "workflows");
        }
    }
}
