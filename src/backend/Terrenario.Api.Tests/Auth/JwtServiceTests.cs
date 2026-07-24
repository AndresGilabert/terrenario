using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Tests.Auth;

public class JwtServiceTests
{
    private static JwtOptions CreateTestOptions()
    {
        using var rsa = RSA.Create(2048);
        return new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenLifetimeSeconds = 900,
            PrivateKeyPem = rsa.ExportRSAPrivateKeyPem(),
            PublicKeyPem = rsa.ExportRSAPublicKeyPem()
        };
    }

    [Fact]
    public void Deberia_EmitirToken_Con_ClaimsCorrectos()
    {
        // Arrange
        var options = CreateTestOptions();
        var sut = new JwtService(Options.Create(options));
        var userId = Guid.NewGuid();

        // Act
        var result = sut.IssueAccessToken(userId, "Test User");

        // Assert
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public void Deberia_ValidarTokenValido_Y_RetornarUserId()
    {
        // Arrange
        var options = CreateTestOptions();
        var sut = new JwtService(Options.Create(options));
        var userId = Guid.NewGuid();
        var issued = sut.IssueAccessToken(userId, "Test User");

        // Act
        var validatedId = sut.ValidateAccessToken(issued.Token);

        // Assert
        validatedId.Should().Be(userId);
    }

    [Fact]
    public void Deberia_RetornarNull_Cuando_TokenEsInvalido()
    {
        // Arrange
        var options = CreateTestOptions();
        var sut = new JwtService(Options.Create(options));

        // Act
        var result = sut.ValidateAccessToken("not-a-valid-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deberia_RetornarNull_Cuando_TokenFirmadoConClaveDiferente()
    {
        // Arrange
        var options1 = CreateTestOptions();
        var options2 = CreateTestOptions();
        var sut1 = new JwtService(Options.Create(options1));
        var sut2 = new JwtService(Options.Create(options2));
        var userId = Guid.NewGuid();

        var tokenFromOtherKey = sut1.IssueAccessToken(userId, null).Token;

        // Act
        var result = sut2.ValidateAccessToken(tokenFromOtherKey);

        // Assert
        result.Should().BeNull();
    }
}
