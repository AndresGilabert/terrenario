using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_tasks_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tasks_workspace_id_is_active",
                table: "tasks",
                columns: new[] { "workspace_id", "is_active" });

            // Unicidad del nombre de tarea dentro del Workspace ignorando mayúsculas (MVP-205): el
            // catálogo existe para dar consistencia (RN-026), así que «Poda» y «poda» son la misma
            // tarea. Es un índice sobre una expresión, que EF Core no sabe declarar en el modelo, por
            // lo que se crea aquí en SQL. Coincide con la comparación de
            // TaskRepository.ExistsWithNameAsync, que da el 409 antes de llegar a la base de datos.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ux_tasks_workspace_name ON tasks (workspace_id, lower(name));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tasks");
        }
    }
}
