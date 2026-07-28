using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "rejected_at",
                table: "workspace_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rejected_by_user_id",
                table: "workspace_invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_invitations_email_status",
                table: "workspace_invitations",
                columns: new[] { "email", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_invitations_rejected_by_user_id",
                table: "workspace_invitations",
                column: "rejected_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workspace_invitations_users_rejected_by_user_id",
                table: "workspace_invitations",
                column: "rejected_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workspace_invitations_users_rejected_by_user_id",
                table: "workspace_invitations");

            migrationBuilder.DropIndex(
                name: "IX_workspace_invitations_email_status",
                table: "workspace_invitations");

            migrationBuilder.DropIndex(
                name: "IX_workspace_invitations_rejected_by_user_id",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "rejected_by_user_id",
                table: "workspace_invitations");
        }
    }
}
