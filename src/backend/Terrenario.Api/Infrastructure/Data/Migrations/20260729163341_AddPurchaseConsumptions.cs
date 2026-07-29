using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// MVP-304 — Consumos e imputaciones. Es la tabla que hace realizable la excepcion mas importante
    /// de la epica (<c>RN-032</c>, hallazgo <c>G-2</c>): <c>purchase_id</c> es <b>anulable</b>, asi que
    /// se puede registrar consumo sin compra previa con coste <c>0</c>.
    ///
    /// Ademas de los campos que el ER anadio en la 3a pasada de <c>MVP-299</c> (<c>date</c>,
    /// <c>season_id</c>, <c>product</c>), se persiste <c>unit_price</c>: el precio unitario congelado
    /// al imputar. Es lo que hace verdadero el CA-3 <i>por estructura</i> —editar la compra despues no
    /// reescribe el coste de lo ya consumido— en vez de por convencion.
    ///
    /// La FK a <c>purchases</c> es <c>RESTRICT</c>: dar de baja una compra no puede llevarse por
    /// delante registros operativos que estan en el diario. La guarda de negocio
    /// (<c>BUSINESS_RULE_PURCHASE_HAS_CONSUMPTIONS</c>) lo impide antes con un 422 explicativo.
    /// </summary>
    public partial class AddPurchaseConsumptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchase_consumptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    product = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    consumed_quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    proportional_cost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_consumptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_consumptions_plots_plot_id",
                        column: x => x.plot_id,
                        principalTable: "plots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_consumptions_purchases_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "purchases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_consumptions_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_consumptions_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_consumptions_live_by_date",
                table: "purchase_consumptions",
                columns: new[] { "workspace_id", "date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_consumptions_plot_id",
                table: "purchase_consumptions",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_consumptions_purchase_id",
                table: "purchase_consumptions",
                column: "purchase_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_consumptions_season_id",
                table: "purchase_consumptions",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_consumptions_workspace_id_plot_id",
                table: "purchase_consumptions",
                columns: new[] { "workspace_id", "plot_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_consumptions_workspace_id_purchase_id",
                table: "purchase_consumptions",
                columns: new[] { "workspace_id", "purchase_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_consumptions");
        }
    }
}
