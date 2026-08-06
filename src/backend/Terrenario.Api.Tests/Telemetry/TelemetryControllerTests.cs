using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Controllers;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

public class TelemetryControllerTests
{
    private readonly IUsageTelemetry _usage = Substitute.For<IUsageTelemetry>();
    private readonly TelemetryController _sut;

    public TelemetryControllerTests() => _sut = new TelemetryController(_usage);

    private const string SessionId = "0123456789abcdef";

    private static WidgetOutcomeRequest Widget(string widget, string status) => new(widget, status);

    [Fact]
    public void Deberia_AceptarLaEntradaAlDashboard_ConSuMarcaDePrimeraVezEnLaSesion()
    {
        var result = _sut.Usage(new UsageTelemetryRequest(
            UsageEvents.DashboardViewed, SessionId, TelemetryDimensions.DeviceDesktop, FirstInSession: true));

        result.Should().BeOfType<AcceptedResult>();
        _usage.Received(1).DashboardViewed(
            Arg.Is<UsageEventContext>(c => c.SessionId == SessionId), true);
    }

    [Fact]
    public void Deberia_TratarLaMarcaAusente_ComoVisitaNoPrimera()
    {
        // Ante la duda, no inflar el numerador del KPI: contar de más una sesión con uso subiría el
        // porcentaje justo en el sentido que interesa al que mide, que es el peor sesgo posible.
        _sut.Usage(new UsageTelemetryRequest(UsageEvents.DashboardViewed, SessionId));

        _usage.Received(1).DashboardViewed(Arg.Any<UsageEventContext>(), false);
    }

    [Fact]
    public void Deberia_RechazarUnEventoDesconocido()
    {
        var result = _sut.Usage(new UsageTelemetryRequest("dashboard_teletransportado", SessionId));

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ApiErrorResponse>()
            .Which.Error.Code.Should().Be(ErrorCodes.ValidationRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sesión con acentos")]
    public void Deberia_DegradarLaSesionADesconocida_SinDescartarLaSenal(string? sessionId)
    {
        var result = _sut.Usage(new UsageTelemetryRequest(UsageEvents.AppSessionStarted, sessionId));

        result.Should().BeOfType<AcceptedResult>();
        _usage.Received(1).AppSessionStarted(
            Arg.Is<UsageEventContext>(c => c.SessionId == TelemetryDimensions.Unknown));
    }

    // ── Cobertura de widgets ─────────────────────────────────────────────────────

    [Fact]
    public void Deberia_AceptarLosWidgetsDelCatalogo()
    {
        _sut.Usage(new UsageTelemetryRequest(UsageEvents.DashboardWidgets, SessionId, Widgets: [
            Widget(DashboardWidgets.Summary, DashboardWidgets.StatusOk),
            Widget(DashboardWidgets.KgByPlot, DashboardWidgets.StatusEmpty),
        ]));

        _usage.Received(1).DashboardWidgets(
            Arg.Any<UsageEventContext>(),
            Arg.Is<IReadOnlyCollection<DashboardWidgetOutcome>>(o => o.Count == 2));
    }

    [Fact]
    public void Deberia_DescartarSoloLoDesconocido_YConservarElResto()
    {
        // Un cliente más nuevo que el servidor puede enviar un widget que este aún no conoce. Tirar el
        // lote entero perdería también los widgets que sí conoce, que es peor que ignorar uno.
        _sut.Usage(new UsageTelemetryRequest(UsageEvents.DashboardWidgets, SessionId, Widgets: [
            Widget("widget_del_futuro", DashboardWidgets.StatusOk),
            Widget(DashboardWidgets.Summary, "medio_ok"),
            Widget(DashboardWidgets.KgByPlot, DashboardWidgets.StatusOk),
        ]));

        _usage.Received(1).DashboardWidgets(
            Arg.Any<UsageEventContext>(),
            Arg.Is<IReadOnlyCollection<DashboardWidgetOutcome>>(o =>
                o.Count == 1 && o.Single().Widget == DashboardWidgets.KgByPlot));
    }

    [Fact]
    public void Deberia_ContarUnaSolaVezCadaWidget_Aunque_LleguenRepetidos()
    {
        // Sin esto, un cliente podría subir la cobertura mandando veinte veces el mismo widget en `ok`.
        _sut.Usage(new UsageTelemetryRequest(UsageEvents.DashboardWidgets, SessionId, Widgets: [
            Widget(DashboardWidgets.Summary, DashboardWidgets.StatusOk),
            Widget(DashboardWidgets.Summary, DashboardWidgets.StatusOk),
            Widget(DashboardWidgets.Summary, DashboardWidgets.StatusOk),
        ]));

        _usage.Received(1).DashboardWidgets(
            Arg.Any<UsageEventContext>(),
            Arg.Is<IReadOnlyCollection<DashboardWidgetOutcome>>(o => o.Count == 1));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Deberia_RechazarLaSenal_Cuando_NoQuedaNingunWidgetReconocible(bool listaAusente)
    {
        var result = _sut.Usage(new UsageTelemetryRequest(
            UsageEvents.DashboardWidgets, SessionId,
            Widgets: listaAusente ? null : [Widget("widget_del_futuro", DashboardWidgets.StatusOk)]));

        result.Should().BeOfType<BadRequestObjectResult>();
        _usage.DidNotReceive().DashboardWidgets(
            Arg.Any<UsageEventContext>(), Arg.Any<IReadOnlyCollection<DashboardWidgetOutcome>>());
    }
}
