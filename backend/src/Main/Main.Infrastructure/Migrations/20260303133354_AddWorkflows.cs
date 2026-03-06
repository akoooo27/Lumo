using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Main.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(30)", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    instruction = table.Column<string>(type: "text", nullable: false),
                    normalized_instruction = table.Column<string>(type: "text", nullable: false),
                    model_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    use_web_search = table.Column<bool>(type: "boolean", nullable: false),
                    delivery_policy = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    pause_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    recurrence_kind = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: false),
                    local_time = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    time_zone_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    next_run_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_run_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    consecutive_failure_count = table.Column<int>(type: "integer", nullable: false),
                    dispatch_lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dispatch_lease_until_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_runs",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(30)", nullable: false),
                    workflow_id = table.Column<string>(type: "varchar(30)", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    result_markdown = table.Column<string>(type: "text", nullable: true),
                    failure_message = table.Column<string>(type: "text", nullable: true),
                    skip_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    model_id_used = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    use_web_search_used = table.Column<bool>(type: "boolean", nullable: false),
                    instruction_snapshot = table.Column<string>(type: "text", nullable: false),
                    title_snapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_runs_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_runs_workflow_id",
                table: "workflow_runs",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_runs_workflow_id_scheduled_for",
                table: "workflow_runs",
                columns: new[] { "workflow_id", "scheduled_for" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflows_status_next_run_at_dispatch_lease_until_utc",
                table: "workflows",
                columns: new[] { "status", "next_run_at", "dispatch_lease_until_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_workflows_user_id_normalized_instruction_recurrence_kind_da",
                table: "workflows",
                columns: new[] { "user_id", "normalized_instruction", "recurrence_kind", "days_of_week_mask", "local_time", "time_zone_id" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_workflows_user_id_status",
                table: "workflows",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_runs");

            migrationBuilder.DropTable(
                name: "workflows");
        }
    }
}
