using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// MVP-303 — Libro de compras. Materializa <c>P-050</c>: <c>season_id</c> es obligatorio
    /// (RN-021) y el ER no lo declaraba, aunque el contrato ya lo exigía. Repite el patrón operativo
    /// que estrenó <c>AddActivities</c>: <c>version</c> como token de concurrencia (ADR-0005),
    /// <c>deleted_at</c> para la baja lógica (RN-037) con índice parcial de vivos, y FK
    /// <c>RESTRICT</c> al maestro, que se inactiva en vez de borrarse.
    ///
    /// <c>unit_price</c> se persiste aunque sea derivable: es lo que <c>MVP-304</c> usará para el
    /// coste proporcional de cada imputación, y guardarlo permite explicar una imputación antigua
    /// aunque la compra se edite después (RN-032, «no se recalculan históricos»).
    /// </summary>
    public partial class AddPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: false),
                    product = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    total_quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchases_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchases_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchases_live_by_date",
                table: "purchases",
                columns: new[] { "workspace_id", "purchase_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_season_id",
                table: "purchases",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_workspace_id_product",
                table: "purchases",
                columns: new[] { "workspace_id", "product" });

            migrationBuilder.CreateIndex(
                name: "IX_purchases_workspace_id_season_id",
                table: "purchases",
                columns: new[] { "workspace_id", "season_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchases");
        }
    }
}
