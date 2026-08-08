using FluentAssertions;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Tests.Emails;

/// <summary>
/// P-100 — Guarda sobre el modo de seguridad SMTP.
///
/// El valor no llega de código sino de una variable de entorno de App Service (`Email__SecurityMode`),
/// y <c>SmtpMailer</c> compara contra constantes en minúscula con una rama por defecto que también es
/// un modo válido: un `None` con mayúscula no rompía, cambiaba el transporte en silencio. Estas
/// pruebas fijan las dos mitades de la solución: que la comparación no dependa de cómo se teclee, y
/// que un valor fuera del catálogo quede marcado como desconocido para que el arranque pueda decirlo.
/// </summary>
public class EmailSecurityModeTests
{
    [Theory]
    [InlineData("None", "none")]
    [InlineData("StartTLS", "starttls")]
    [InlineData("  ssl  ", "ssl")]
    [InlineData("AUTO", "auto")]
    public void UnModoValido_DebeReconocerse_SeaComoSeaQueSeTeclee(string configured, string expected)
    {
        var options = new EmailOptions { SecurityMode = configured };

        options.NormalizedSecurityMode.Should().Be(expected);
        options.IsSecurityModeKnown.Should().BeTrue();
    }

    [Theory]
    [InlineData("tls")]
    [InlineData("starttls!")]
    [InlineData("")]
    public void UnModoFueraDelCatalogo_DebeQuedarMarcadoComoDesconocido(string configured)
        // No basta con que caiga al defecto: el arranque avisa a partir de esta comprobación, y sin
        // ella el diagnóstico vuelve a aparecer en la primera entrega fallida.
        => new EmailOptions { SecurityMode = configured }.IsSecurityModeKnown.Should().BeFalse();

    [Fact]
    public void ElDefectoDeLaClase_DebeSerUnModoDelCatalogo()
        => new EmailOptions().IsSecurityModeKnown.Should().BeTrue();
}
