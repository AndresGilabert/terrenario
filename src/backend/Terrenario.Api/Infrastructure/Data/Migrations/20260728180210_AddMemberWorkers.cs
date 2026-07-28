using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// MVP-208 (CA-1/CA-5) — Materializa a los miembros del Workspace como filas de <c>workers</c>,
    /// que es lo que cierra <c>P-034</c>: a partir de aquí cualquier responsable —miembro o
    /// cuadrilla— se identifica con un <c>workers.id</c> y puede guardarse en
    /// <c>ACTIVITY.worker_id</c> sin campos alternativos.
    ///
    /// El backfill sigue la misma política de datos preexistentes que aprobó el PO en MVP-207:
    /// <b>conservar y renombrar, nunca borrar ni hacer fallar la migración</b> (la API migra al
    /// arrancar, así que un fallo aquí deja el entorno sin levantar). Si un trabajador de cuadrilla
    /// ocupaba el nombre de un miembro, el nombre lo conserva el miembro —no es renombrable, llega de
    /// su cuenta de Google (RN-036)— y la fila de cuadrilla recibe el primer sufijo libre.
    /// </summary>
    public partial class AddMemberWorkers : Migration
    {
        /// <summary>Longitud máxima de <c>workers.name</c>: el sufijo de desempate no puede desbordarla.</summary>
        private const int NameMaxLength = 150;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── CA-5 · backfill de los miembros actuales ────────────────────────────────────────
            // Se materializan solo los miembros **activos**: un acceso revocado antes de MVP-208 no
            // tiene registros que lo referencien (la operativa diaria es MVP-003), así que no hay nada
            // que preservar. Los Workspaces dados de baja sí entran: la baja es reversible (MVP-206) y
            // al reabrirse deben volver con su maestro completo.
            migrationBuilder.Sql(
                $"""
                CREATE TEMP TABLE responsables_pendientes ON COMMIT DROP AS
                SELECT m.workspace_id,
                       m.user_id,
                       m.joined_at,
                       left(btrim(u.display_name), {NameMaxLength}) AS nombre
                  FROM workspace_members AS m
                  JOIN users AS u ON u.id = m.user_id
                 WHERE m.status = 'activo'
                   AND NOT EXISTS (
                       SELECT 1 FROM workers AS w
                        WHERE w.workspace_id = m.workspace_id
                          AND w.user_account_id = m.user_id);

                -- Dos cuentas de Google con el mismo nombre de display en el mismo Workspace: ninguna
                -- es renombrable, así que el sufijo lo toma quien entró después. Sin esto, el índice
                -- ux_workers_workspace_name rechazaría la segunda fila.
                UPDATE responsables_pendientes AS p
                   SET nombre = left(p.nombre, {NameMaxLength} - length(' (' || d.orden || ')'))
                                || ' (' || d.orden || ')'
                  FROM (SELECT workspace_id,
                               user_id,
                               row_number() OVER (
                                   PARTITION BY workspace_id, lower(nombre)
                                   ORDER BY joined_at, user_id) AS orden
                          FROM responsables_pendientes) AS d
                 WHERE d.workspace_id = p.workspace_id
                   AND d.user_id = p.user_id
                   AND d.orden > 1;

                -- La cuadrilla cede el nombre al miembro. El sufijo se busca con el primer ordinal
                -- libre —comprobando a la vez el maestro actual y los nombres que van a ocupar los
                -- miembros— porque el índice único de MVP-207 ya está creado y un nombre repetido
                -- haría fallar el UPDATE.
                UPDATE workers AS w
                   SET name = candidatos.nombre,
                       updated_at = now()
                  FROM (
                      SELECT ocupante.id,
                             (SELECT left(ocupante.name, {NameMaxLength} - length(' (' || n || ')'))
                                     || ' (' || n || ')'
                                FROM generate_series(2, 99) AS n
                               WHERE NOT EXISTS (
                                     SELECT 1 FROM workers AS otro
                                      WHERE otro.workspace_id = ocupante.workspace_id
                                        AND lower(otro.name) = lower(
                                            left(ocupante.name, {NameMaxLength} - length(' (' || n || ')'))
                                            || ' (' || n || ')'))
                                 AND NOT EXISTS (
                                     SELECT 1 FROM responsables_pendientes AS p
                                      WHERE p.workspace_id = ocupante.workspace_id
                                        AND lower(p.nombre) = lower(
                                            left(ocupante.name, {NameMaxLength} - length(' (' || n || ')'))
                                            || ' (' || n || ')'))
                               LIMIT 1) AS nombre
                        FROM workers AS ocupante
                       WHERE ocupante.user_account_id IS NULL
                         AND EXISTS (
                             SELECT 1 FROM responsables_pendientes AS p
                              WHERE p.workspace_id = ocupante.workspace_id
                                AND lower(p.nombre) = lower(ocupante.name))
                  ) AS candidatos
                 WHERE candidatos.id = w.id
                   AND candidatos.nombre IS NOT NULL;

                INSERT INTO workers (
                    id, workspace_id, user_account_id, name, hourly_rate, is_active, created_at, updated_at)
                SELECT gen_random_uuid(), p.workspace_id, p.user_id, p.nombre, NULL, true, now(), now()
                  FROM responsables_pendientes AS p;
                """);

            // CA-1 · una cuenta tiene como mucho una fila de responsable por Workspace. Se crea
            // después del backfill para que valide el resultado, no un estado intermedio.
            migrationBuilder.CreateIndex(
                name: "ux_workers_workspace_user_account",
                table: "workers",
                columns: new[] { "workspace_id", "user_account_id" },
                unique: true,
                filter: "user_account_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Solo se retira el índice. Ni las filas materializadas ni el renombrado de la cuadrilla se
            // deshacen, con el mismo criterio que MVP-207: son datos de maestro válidos y no hay forma
            // de distinguir los nombres originales de los que el usuario haya elegido después. Borrar
            // las filas dejaría además sin responsable a los registros que ya las referencien.
            migrationBuilder.DropIndex(
                name: "ux_workers_workspace_user_account",
                table: "workers");
        }
    }
}
