using FluentAssertions;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Auth;

public class LoginFunnelEventsTests
{
    [Theory]
    [InlineData("ab12CD34")]
    [InlineData("0123456789abcdef0123456789abcdef")]
    public void IsValidFlowId_EsTrue_Cuando_EsAlfanumericoYAcotado(string flowId)
    {
        LoginFunnelEvents.IsValidFlowId(flowId).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("con espacio")]
    [InlineData("salto\nlinea")]
    [InlineData("guion-bajo_no")]
    public void IsValidFlowId_EsFalse_Cuando_TieneCaracteresNoPermitidos(string? flowId)
    {
        LoginFunnelEvents.IsValidFlowId(flowId).Should().BeFalse();
    }

    [Fact]
    public void IsValidFlowId_EsFalse_Cuando_ExcedeLongitudMaxima()
    {
        var demasiadoLargo = new string('a', LoginFunnelEvents.FlowIdMaxLength + 1);

        LoginFunnelEvents.IsValidFlowId(demasiadoLargo).Should().BeFalse();
    }

    [Fact]
    public void ClientIngestable_ContieneLosEventosDeCliente()
    {
        LoginFunnelEvents.ClientIngestable.Should().BeEquivalentTo(new[]
        {
            LoginFunnelEvents.ScreenViewed,
            LoginFunnelEvents.GoogleClicked,
            LoginFunnelEvents.Abandonment,
        });
    }

    [Fact]
    public void ClientIngestable_ExcluyeExitoYError_QueSonAutoritativosDelServidor()
    {
        LoginFunnelEvents.ClientIngestable.Should().NotContain(LoginFunnelEvents.Success);
        LoginFunnelEvents.ClientIngestable.Should().NotContain(LoginFunnelEvents.Error);
    }
}
