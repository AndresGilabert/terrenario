using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

public class TelemetryFlushWorkerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Ahora);
    private readonly ITelemetryCounterStore _store = Substitute.For<ITelemetryCounterStore>();
    private readonly TelemetryCounterAccumulator _accumulator;
    private readonly TelemetryFlushWorker _sut;

    public TelemetryFlushWorkerTests()
    {
        _accumulator = new TelemetryCounterAccumulator(_clock);

        var services = new ServiceCollection();
        services.AddScoped(_ => _store);

        _sut = new TelemetryFlushWorker(
            services.BuildServiceProvider(),
            _accumulator,
            Options.Create(new TelemetryOptions()),
            _clock,
            NullLogger<TelemetryFlushWorker>.Instance);
    }

    [Fact]
    public async Task Deberia_VolcarLoAcumulado()
    {
        _accumulator.Add("login.success", 3);

        await _sut.FlushOnceAsync(CancellationToken.None);

        await _store.Received(1).AddAsync(
            Arg.Is<IReadOnlyCollection<TelemetryCounter>>(c => c.Single().Value == 3), Ahora, Arg.Any<CancellationToken>());
        _accumulator.Drain().Should().BeEmpty();
    }

    [Fact]
    public async Task Deberia_DevolverLoDrenadoAlAcumulador_Cuando_LaEscrituraFalla()
    {
        // Un fallo de base de datos no puede costar la ventana medida: se reintenta en la siguiente
        // pasada con lo de entonces sumado.
        _store.AddAsync(Arg.Any<IReadOnlyCollection<TelemetryCounter>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("base caída"));
        _accumulator.Add("login.success", 3);

        await _sut.FlushOnceAsync(CancellationToken.None);

        _accumulator.Drain().Single().Value.Should().Be(3);
    }

    [Fact]
    public async Task Deberia_PodarUnaVezAlDia_YNoEnCadaPasada()
    {
        await _sut.FlushOnceAsync(CancellationToken.None);
        await _sut.FlushOnceAsync(CancellationToken.None);

        await _store.Received(1).PruneAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());

        _clock.Advance(TimeSpan.FromDays(1));
        await _sut.FlushOnceAsync(CancellationToken.None);

        await _store.Received(2).PruneAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_PodarPorLaVentanaConfigurada()
    {
        await _sut.FlushOnceAsync(CancellationToken.None);

        var esperado = DateOnly.FromDateTime(Ahora.UtcDateTime).AddDays(-new TelemetryOptions().RetentionDays);
        await _store.Received(1).PruneAsync(esperado, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoEscribirNada_Cuando_NoHaOcurridoNada()
    {
        await _sut.FlushOnceAsync(CancellationToken.None);

        await _store.DidNotReceive().AddAsync(
            Arg.Any<IReadOnlyCollection<TelemetryCounter>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
