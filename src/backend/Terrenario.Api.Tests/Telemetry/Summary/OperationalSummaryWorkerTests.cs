using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Terrenario.Api.Application.Ops;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Invitations;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;
using Terrenario.Api.Infrastructure.Telemetry.Summary;
using Terrenario.Api.Tests.Telemetry;

namespace Terrenario.Api.Tests.Telemetry.Summary;

/// <summary>
/// MKT-101 — Cadencia del resumen operativo: una vez al día a partir de las 05:00 (Europe/Madrid), y
/// el semanal solo los lunes. No se comprueba el cuerpo del correo aquí —eso lo hace
/// <c>ProductEmailInventoryTests</c> sobre el catálogo—, sino cuándo se intenta el envío y que un
/// destinatario ausente o un fallo de consulta no tumban el proceso (CA-3).
/// </summary>
public class OperationalSummaryWorkerTests
{
    // Lunes 2026-08-31, 03:01 UTC == 05:01 Europe/Madrid (CEST, UTC+2): ya ha pasado la hora de envío.
    private static readonly DateTimeOffset LunesTrasLaHora = new(2026, 8, 31, 3, 1, 0, TimeSpan.Zero);

    // Mismo lunes, pero antes de las 05:00 locales.
    private static readonly DateTimeOffset LunesAntesDeLaHora = new(2026, 8, 31, 2, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(LunesAntesDeLaHora);
    private readonly ITelemetryCounterStore _store = Substitute.For<ITelemetryCounterStore>();
    private readonly RecordingLogger<OperationalSummaryWorker> _logger = new();

    public OperationalSummaryWorkerTests() =>
        _store.GetRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TelemetryCounter>>([]));

    private OperationalSummaryWorker CreateSut(string alertEmail = "")
    {
        var services = new ServiceCollection();
        services.AddSingleton(_store);
        services.AddSingleton(new RollingWindowMetrics(_clock));
        services.AddSingleton(new AlertStateStore());
        services.AddSingleton<TimeProvider>(_clock);
        services.AddScoped<OperationalSignalsService>();
        services.AddScoped(_ => new SmtpMailer(Options.Create(new EmailOptions()), NullLogger<SmtpMailer>.Instance));
        services.AddScoped(_ => new ProductEmailTemplate(
            Options.Create(new EmailOptions()), Options.Create(new LegalEntityOptions())));

        return new OperationalSummaryWorker(
            services.BuildServiceProvider(),
            Options.Create(new OpsOptions { SummaryEnabled = true, AlertEmail = alertEmail }),
            _clock,
            _logger);
    }

    private int LlamadasAlAlmacen() => _store.ReceivedCalls()
        .Count(call => call.GetMethodInfo().Name == nameof(ITelemetryCounterStore.GetRangeAsync));

    [Fact]
    public async Task Deberia_NoHacerNada_Antes_De_LaHoraDeEnvio()
    {
        var sut = CreateSut();

        await sut.RunOnceAsync(CancellationToken.None);

        LlamadasAlAlmacen().Should().Be(0);
    }

    [Fact]
    public async Task Deberia_ConsultarLasSenales_UnaSolaVez_AunqueSeLlameVariasVecesElMismoDia()
    {
        _clock.SetUtcNow(LunesTrasLaHora);
        var sut = CreateSut();

        await sut.RunOnceAsync(CancellationToken.None);
        LlamadasAlAlmacen().Should().Be(1);

        await sut.RunOnceAsync(CancellationToken.None);
        LlamadasAlAlmacen().Should().Be(1);

        _clock.Advance(TimeSpan.FromDays(1));
        await sut.RunOnceAsync(CancellationToken.None);
        LlamadasAlAlmacen().Should().Be(2);
    }

    [Fact]
    public async Task Deberia_IntentarElResumenDiarioYElSemanal_SoloEnLunes()
    {
        _clock.SetUtcNow(LunesTrasLaHora);
        var sut = CreateSut();

        await sut.RunOnceAsync(CancellationToken.None);

        _logger.Entries.Should().Contain(e => Equals(e.GetValueOrDefault("Context"), "resumen-operativo-diario"));
        _logger.Entries.Should().Contain(e => Equals(e.GetValueOrDefault("Context"), "resumen-operativo-semanal"));
    }

    [Fact]
    public async Task Deberia_NoIntentarElResumenSemanal_FueraDeLunes()
    {
        // Martes 2026-09-01, 05:01 Europe/Madrid.
        _clock.SetUtcNow(new DateTimeOffset(2026, 9, 1, 3, 1, 0, TimeSpan.Zero));
        var sut = CreateSut();

        await sut.RunOnceAsync(CancellationToken.None);

        _logger.Entries.Should().Contain(e => Equals(e.GetValueOrDefault("Context"), "resumen-operativo-diario"));
        _logger.Entries.Should().NotContain(e => Equals(e.GetValueOrDefault("Context"), "resumen-operativo-semanal"));
    }

    [Fact]
    public async Task Deberia_NoPropagarLaExcepcion_Cuando_FallaLaConsultaDeSenales()
    {
        _clock.SetUtcNow(LunesTrasLaHora);
        _store.GetRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<TelemetryCounter>>>(_ => throw new InvalidOperationException("base caída"));
        var sut = CreateSut();

        var act = () => sut.RunOnceAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        // Al no completarse el envío, no se marca como hecho: la siguiente pasada vuelve a intentarlo.
        await sut.RunOnceAsync(CancellationToken.None);
        LlamadasAlAlmacen().Should().Be(2);
    }
}
