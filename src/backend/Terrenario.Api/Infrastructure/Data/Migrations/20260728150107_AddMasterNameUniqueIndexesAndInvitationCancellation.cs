using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// MVP-207 — Correcciones de cierre de la épica de maestros:
    /// <list type="bullet">
    ///   <item>
    ///     CA-3: nombre único por Workspace ignorando mayúsculas en <c>seasons</c>, <c>workers</c> y
    ///     <c>plots</c>, con el mismo patrón que <c>ux_tasks_workspace_name</c> (MVP-205). Antes de
    ///     crear cada índice se resuelven los duplicados preexistentes (decisión del PO, ver Up).
    ///   </item>
    ///   <item>
    ///     CA-4: <c>cancelled_at</c> / <c>cancelled_by_user_id</c> en <c>workspace_invitations</c>
    ///     para el estado <c>anulada</c> (anulación de una invitación pendiente por el emisor).
    ///   </item>
    /// </list>
    /// </summary>
    public partial class AddMasterNameUniqueIndexesAndInvitationCancellation : Migration
    {
        /// <summary>
        /// Nombre del índice único y longitud máxima de la columna <c>name</c> de cada maestro. La
        /// longitud se usa al renombrar duplicados, para que el sufijo no desborde el <c>varchar</c>.
        /// </summary>
        private static readonly (string Table, string IndexName, int NameMaxLength)[] MasterTables =
        [
            ("seasons", "ux_seasons_workspace_name", 120),
            ("workers", "ux_workers_workspace_name", 150),
            ("plots", "ux_plots_workspace_name", 150)
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "workspace_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_user_id",
                table: "workspace_invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_invitations_cancelled_by_user_id",
                table: "workspace_invitations",
                column: "cancelled_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workspace_invitations_users_cancelled_by_user_id",
                table: "workspace_invitations",
                column: "cancelled_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // ── CA-2/CA-3: nombre único por Workspace en los tres maestros que faltaban ──────────
            // El índice no se puede crear sobre una tabla que ya contenga duplicados, y en desarrollo
            // los hay (la revisión MVP-299 creó tres «Juan Perez», dos «Prueba» y dos «2025/2026»).
            // Decisión del PO: **renombrar**, no borrar ni inactivar. Se conserva intacto el registro
            // más antiguo de cada grupo y el resto recibe un sufijo « (2)», « (3)»… por orden de
            // created_at; el usuario los renombra o inactiva después desde la UI. Inactivarlos no
            // habría servido: la guarda cubre todo el maestro, así que las filas inactivas también
            // ocupan su nombre.
            //
            // El bucle repite el renombrado porque un nombre generado puede chocar con uno que ya
            // existía («Poda» duplicado junto a un «Poda (2)» previo); converge en una o dos vueltas y
            // el guard evita cualquier bucle infinito.
            foreach (var (table, indexName, nameMaxLength) in MasterTables)
            {
                migrationBuilder.Sql(
                    $"""
                    DO $$
                    DECLARE
                        afectadas integer;
                        vueltas integer := 0;
                    BEGIN
                        LOOP
                            WITH duplicados AS (
                                SELECT id,
                                       row_number() OVER (
                                           PARTITION BY workspace_id, lower(name)
                                           ORDER BY created_at, id) AS orden
                                  FROM {table}
                            )
                            UPDATE {table} AS m
                               SET name = left(m.name, {nameMaxLength} - length(' (' || d.orden || ')'))
                                          || ' (' || d.orden || ')',
                                   updated_at = now()
                              FROM duplicados AS d
                             WHERE d.id = m.id
                               AND d.orden > 1;

                            GET DIAGNOSTICS afectadas = ROW_COUNT;
                            vueltas := vueltas + 1;
                            EXIT WHEN afectadas = 0 OR vueltas >= 10;
                        END LOOP;
                    END $$;
                    """);

                // Índice sobre una expresión: EF Core no sabe declararlo en el modelo (el DbContext lo
                // documenta en su lugar). Es la misma comparación que hace
                // {table}Repository.ExistsWithNameAsync, que da el 409 antes de llegar aquí.
                migrationBuilder.Sql(
                    $"CREATE UNIQUE INDEX {indexName} ON {table} (workspace_id, lower(name));");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Los índices se retiran, pero el renombrado de duplicados no se deshace: no hay forma de
            // saber qué nombres eran originales y cuáles los eligió después el usuario.
            foreach (var (_, indexName, _) in MasterTables)
                migrationBuilder.Sql($"DROP INDEX IF EXISTS {indexName};");

            migrationBuilder.DropForeignKey(
                name: "FK_workspace_invitations_users_cancelled_by_user_id",
                table: "workspace_invitations");

            migrationBuilder.DropIndex(
                name: "IX_workspace_invitations_cancelled_by_user_id",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "workspace_invitations");

            migrationBuilder.DropColumn(
                name: "cancelled_by_user_id",
                table: "workspace_invitations");
        }
    }
}
