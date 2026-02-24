using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Main.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendMemoryRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_memories_user_id_created_at",
                table: "memories");

            migrationBuilder.AddColumn<int>(
                name: "access_count",
                table: "memories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "importance",
                table: "memories",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "memories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_accessed_at",
                table: "memories",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "memories",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_memories_user_id_is_active_created_at",
                table: "memories",
                columns: new[] { "user_id", "is_active", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_memories_user_id_is_active_created_at",
                table: "memories");

            migrationBuilder.DropColumn(
                name: "access_count",
                table: "memories");

            migrationBuilder.DropColumn(
                name: "importance",
                table: "memories");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "memories");

            migrationBuilder.DropColumn(
                name: "last_accessed_at",
                table: "memories");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "memories");

            migrationBuilder.CreateIndex(
                name: "ix_memories_user_id_created_at",
                table: "memories",
                columns: new[] { "user_id", "created_at" });
        }
    }
}
