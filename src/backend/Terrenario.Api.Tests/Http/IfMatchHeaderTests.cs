using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Terrenario.Api.Common.Http;

namespace Terrenario.Api.Tests.Http;

/// <summary>
/// Lectura de <c>If-Match</c> en los registros operativos (ADR-0005, MVP-301). El contrato publica la
/// versión como entero, pero un cliente HTTP correcto puede mandarla en forma de ETag: se aceptan las
/// tres formas y se rechaza <c>*</c>, que significa «cualquier versión».
/// </summary>
public class IfMatchHeaderTests
{
    private static IHeaderDictionary Headers(string? value)
    {
        IHeaderDictionary headers = new HeaderDictionary();
        if (value is not null) headers.IfMatch = value;
        return headers;
    }

    [Theory]
    [InlineData("3")]
    [InlineData("\"3\"")]
    [InlineData("W/\"3\"")]
    [InlineData("  3  ")]
    public void TryRead_Deberia_AceptarLasFormasValidas(string raw)
    {
        IfMatchHeader.TryRead(Headers(raw), out var version).Should().BeTrue();
        version.Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("*")]
    [InlineData("\"*\"")]
    [InlineData("abc")]
    [InlineData("3.5")]
    [InlineData("-1")]
    public void TryRead_Deberia_RechazarLoQueNoEsUnaVersion(string? raw)
    {
        IfMatchHeader.TryRead(Headers(raw), out _).Should().BeFalse();
    }
}
