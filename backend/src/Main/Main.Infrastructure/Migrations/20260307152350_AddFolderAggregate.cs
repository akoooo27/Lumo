using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Main.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "folder_id",
                table: "chats",
                type: "varchar(30)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "folders",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(30)", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    normalized_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_folders", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chats_user_id_folder_id_updated_at",
                table: "chats",
                columns: new[] { "user_id", "folder_id", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_folders_user_id_normalized_name",
                table: "folders",
                columns: new[] { "user_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_folders_user_id_sort_order",
                table: "folders",
                columns: new[] { "user_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "folders");

            migrationBuilder.DropIndex(
                name: "ix_chats_user_id_folder_id_updated_at",
                table: "chats");

            migrationBuilder.DropColumn(
                name: "folder_id",
                table: "chats");
        }
    }
}
