using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// MVP-301 — Primera entidad operativa del producto. Aporta al esquema tres cosas que el resto de
    /// la épica (compras y consumos) reutiliza:
    /// <list type="bullet">
    /// <item><c>version</c> (bigint) como token de concurrencia optimista (ADR-0005).</item>
    /// <item><c>deleted_at</c> para la eliminación lógica (RN-037), con índice parcial
    /// <c>ix_activities_live_by_date</c> porque el 100% de las lecturas filtra por «vivo».</item>
    /// <item><c>task_id</c> (FK opcional al catálogo) + <c>task_text</c>, que cierran <c>P-028</c>:
    /// el ER declaraba la tarea como un <c>string task</c> suelto anterior al catálogo de MVP-205.</item>
    /// </list>
    /// </summary>
    public partial class AddActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    task_text = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    manual_cost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_activities_plots_plot_id",
                        column: x => x.plot_id,
                        principalTable: "plots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activities_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activities_live_by_date",
                table: "activities",
                columns: new[] { "workspace_id", "date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_activities_plot_id",
                table: "activities",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_season_id",
                table: "activities",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_task_id",
                table: "activities",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_worker_id",
                table: "activities",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_workspace_id_plot_id",
                table: "activities",
                columns: new[] { "workspace_id", "plot_id" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_workspace_id_season_id",
                table: "activities",
                columns: new[] { "workspace_id", "season_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activities");
        }
    }
}
