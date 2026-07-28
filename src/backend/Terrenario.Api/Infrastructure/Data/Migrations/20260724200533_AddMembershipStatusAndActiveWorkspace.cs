using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipStatusAndActiveWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspace_members_user_id",
                table: "workspace_members");

            // Se añade primero sin valor por defecto persistido y se rellena desde is_active,
            // de modo que las membresías ya existentes conservan su estado (catálogo
            // worker_member_status: 'activo' | 'revocado') en lugar de quedar en blanco.
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "workspace_members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "activo");

            migrationBuilder.Sql(
                "UPDATE workspace_members SET status = CASE WHEN is_active THEN 'activo' ELSE 'revocado' END;");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "workspace_members");

            migrationBuilder.AddColumn<Guid>(
                name: "active_workspace_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_user_id_status",
                table: "workspace_members",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_users_active_workspace_id",
                table: "users",
                column: "active_workspace_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_workspaces_active_workspace_id",
                table: "users",
                column: "active_workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_workspaces_active_workspace_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_workspace_members_user_id_status",
                table: "workspace_members");

            migrationBuilder.DropIndex(
                name: "IX_users_active_workspace_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "status",
                table: "workspace_members");

            migrationBuilder.DropColumn(
                name: "active_workspace_id",
                table: "users");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "workspace_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_user_id",
                table: "workspace_members",
                column: "user_id");
        }
    }
}
