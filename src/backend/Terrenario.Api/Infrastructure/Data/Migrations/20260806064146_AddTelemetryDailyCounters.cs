using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terrenario.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryDailyCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telemetry_daily_counters",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    metric = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    value = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_daily_counters", x => new { x.date, x.metric });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telemetry_daily_counters");
        }
    }
}
