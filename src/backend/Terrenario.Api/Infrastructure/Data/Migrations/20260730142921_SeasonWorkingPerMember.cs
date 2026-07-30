using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeasonWorkingPerMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MVP-209 — La temporada de trabajo pasa de ser un flag por Workspace (seasons.is_active +
            // ux_seasons_workspace_active) a una preferencia por usuario en
            // workspace_members.active_season_id. El orden importa: primero se crea la columna nueva y se
            // **rellena leyendo is_active**, y solo después se elimina el índice y la columna antiguos,
            // para no perder qué temporada estaba activa en cada Workspace.

            // 1) Columna + índice + FK (ON DELETE SET NULL: borrar la temporada devuelve al defecto).
            migrationBuilder.AddColumn<Guid>(
                name: "active_season_id",
                table: "workspace_members",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_active_season_id",
                table: "workspace_members",
                column: "active_season_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workspace_members_seasons_active_season_id",
                table: "workspace_members",
                column: "active_season_id",
                principalTable: "seasons",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // 2) Backfill: cada miembro hereda la temporada hoy activa de su Workspace, para no cambiar
            //    el comportamiento visible de nadie (CA-5). Los Workspaces sin activa quedan en NULL y
            //    resuelven el defecto (WorkingSeasonPolicy).
            migrationBuilder.Sql(@"
                UPDATE workspace_members m
                SET active_season_id = s.id
                FROM seasons s
                WHERE s.workspace_id = m.workspace_id AND s.is_active = true;");

            // 3) Ya migrado el dato, se retira el modelo antiguo: el índice único parcial de «una activa
            //    por Workspace» y la columna is_active.
            migrationBuilder.DropIndex(
                name: "ux_seasons_workspace_active",
                table: "seasons");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "seasons");

            // 4) El índice único parcial hacía además de índice de acceso por workspace_id; al retirarlo,
            //    se crea el índice simple que la FK a workspaces necesita.
            migrationBuilder.CreateIndex(
                name: "IX_seasons_workspace_id",
                table: "seasons",
                column: "workspace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workspace_members_seasons_active_season_id",
                table: "workspace_members");

            migrationBuilder.DropIndex(
                name: "IX_workspace_members_active_season_id",
                table: "workspace_members");

            migrationBuilder.DropIndex(
                name: "IX_seasons_workspace_id",
                table: "seasons");

            migrationBuilder.DropColumn(
                name: "active_season_id",
                table: "workspace_members");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ux_seasons_workspace_active",
                table: "seasons",
                column: "workspace_id",
                unique: true,
                filter: "is_active");
        }
    }
}
