using FluentAssertions;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-601 — El almacén de contadores, contra PostgreSQL real.
///
/// Aquí el motor real es lo único que prueba algo: toda la corrección del volcado está en un
/// <c>INSERT … ON CONFLICT DO UPDATE</c> que suma en el propio motor. Con un doble en memoria se
/// estaría comprobando el doble, no la sentencia que corre en producción.
/// </summary>
public sealed class TelemetryCounterStoreTests : RepositoryTestBase
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(Ahora.UtcDateTime);

    private TelemetryCounterStore NewStore() => new(NewDb());

    [Fact]
    public async Task Deberia_GuardarLosContadoresDeUnDia()
    {
        await NewStore().AddAsync(
            [new TelemetryCounter(Hoy, "login.screen_viewed", 7)], Ahora, CancellationToken.None);

        var leidos = await NewStore().GetRangeAsync(Hoy, Hoy, CancellationToken.None);

        leidos.Should().ContainSingle()
            .Which.Should().Be(new TelemetryCounter(Hoy, "login.screen_viewed", 7));
    }

    [Fact]
    public async Task Deberia_SumarSobreLoYaGuardado_YNoSustituirlo()
    {
        // Cada volcado trae lo ocurrido desde el anterior. Si sustituyera, cada minuto borraría el
        // acumulado del día y el KPI diario sería el del último minuto.
        await NewStore().AddAsync([new TelemetryCounter(Hoy, "login.success", 4)], Ahora, CancellationToken.None);
        await NewStore().AddAsync([new TelemetryCounter(Hoy, "login.success", 6)], Ahora, CancellationToken.None);

        var leidos = await NewStore().GetRangeAsync(Hoy, Hoy, CancellationToken.None);

        leidos.Single().Value.Should().Be(10);
    }

    [Fact]
    public async Task Deberia_MantenerSeparadosLosDiasYLasMetricas()
    {
        var ayer = Hoy.AddDays(-1);
        await NewStore().AddAsync(
            [
                new TelemetryCounter(ayer, "login.success", 1),
                new TelemetryCounter(Hoy, "login.success", 2),
                new TelemetryCounter(Hoy, "login.abandonment", 3),
            ], Ahora, CancellationToken.None);

        var leidos = await NewStore().GetRangeAsync(ayer, Hoy, CancellationToken.None);

        leidos.Should().BeEquivalentTo(new[]
        {
            new TelemetryCounter(ayer, "login.success", 1),
            new TelemetryCounter(Hoy, "login.abandonment", 3),
            new TelemetryCounter(Hoy, "login.success", 2),
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Deberia_DevolverSoloElRangoPedido()
    {
        await NewStore().AddAsync(
            [
                new TelemetryCounter(Hoy.AddDays(-10), "login.success", 1),
                new TelemetryCounter(Hoy, "login.success", 2),
            ], Ahora, CancellationToken.None);

        var leidos = await NewStore().GetRangeAsync(Hoy.AddDays(-6), Hoy, CancellationToken.None);

        leidos.Should().ContainSingle().Which.Date.Should().Be(Hoy);
    }

    [Fact]
    public async Task Deberia_PodarLoAnteriorALaVentana_YConservarElResto()
    {
        await NewStore().AddAsync(
            [
                new TelemetryCounter(Hoy.AddDays(-400), "login.success", 1),
                new TelemetryCounter(Hoy.AddDays(-30), "login.success", 2),
            ], Ahora, CancellationToken.None);

        var borrados = await NewStore().PruneAsync(Hoy.AddDays(-100), CancellationToken.None);

        borrados.Should().Be(1);
        var leidos = await NewStore().GetRangeAsync(Hoy.AddDays(-500), Hoy, CancellationToken.None);
        leidos.Should().ContainSingle().Which.Date.Should().Be(Hoy.AddDays(-30));
    }

    [Fact]
    public async Task Deberia_NoTocarLaBase_Cuando_NoHayNadaQueVolcar()
    {
        await NewStore().AddAsync([], Ahora, CancellationToken.None);

        var leidos = await NewStore().GetRangeAsync(Hoy.AddDays(-1), Hoy, CancellationToken.None);
        leidos.Should().BeEmpty();
    }
}
