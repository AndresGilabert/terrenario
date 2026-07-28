using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "workspaces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "workspaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workspace_reactivation_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authorizer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_reactivation_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_workspace_reactivation_requests_users_authorizer_user_id",
                        column: x => x.authorizer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workspace_reactivation_requests_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workspace_reactivation_requests_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_deleted_by_user_id",
                table: "workspaces",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_live",
                table: "workspaces",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_reactivation_requests_authorizer_user_id_status",
                table: "workspace_reactivation_requests",
                columns: new[] { "authorizer_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_reactivation_requests_recipient_user_id",
                table: "workspace_reactivation_requests",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_reactivation_requests_token_hash",
                table: "workspace_reactivation_requests",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_reactivation_requests_workspace_id_status",
                table: "workspace_reactivation_requests",
                columns: new[] { "workspace_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "FK_workspaces_users_deleted_by_user_id",
                table: "workspaces",
                column: "deleted_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workspaces_users_deleted_by_user_id",
                table: "workspaces");

            migrationBuilder.DropTable(
                name: "workspace_reactivation_requests");

            migrationBuilder.DropIndex(
                name: "IX_workspaces_deleted_by_user_id",
                table: "workspaces");

            migrationBuilder.DropIndex(
                name: "ix_workspaces_live",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "workspaces");
        }
    }
}
