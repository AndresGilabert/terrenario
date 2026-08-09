using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Terrenario.Api.Application.Feedback;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Controllers;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Infrastructure.Feedback;

namespace Terrenario.Api.Tests.Feedback;

/// <summary>
/// MVP-711 — La puerta del canal: qué se acepta, qué se recorta antes de que llegue al buzón y qué
/// se responde cuando no se puede enviar.
/// </summary>
public class FeedbackControllerTests
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/128.0";

    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IFeedbackEmailSender _sender = Substitute.For<IFeedbackEmailSender>();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero));
    private readonly FeedbackRateLimiter _limiter;

    public FeedbackControllerTests()
    {
        _limiter = new FeedbackRateLimiter(_clock);

        _users.FindByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(User.Create("google-sub", "Antonio", "antonio@ejemplo.com"));

        _sender.IsEnabled.Returns(true);
    }

    private FeedbackController CreateSut(string recipient = "operacion@ejemplo.com")
    {
        var handler = new SubmitFeedbackHandler(
            _users,
            _sender,
            Options.Create(new FeedbackOptions { Recipient = recipient }),
            NullLogger<SubmitFeedbackHandler>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, UserId.ToString())], "test"));
        httpContext.Request.Headers.UserAgent = UserAgent;

        return new FeedbackController(handler, _limiter, NullLogger<FeedbackController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static FeedbackRequest Request(
        string? kind = FeedbackKinds.Incident,
        string? message = "No puedo guardar una labor.",
        string? path = "/app/diario",
        string? requestId = null) => new(kind, message, path, requestId);

    private FeedbackEmail Captured() => (FeedbackEmail)_sender.ReceivedCalls()
        .Single(call => call.GetMethodInfo().Name == nameof(IFeedbackEmailSender.SendAsync))
        .GetArguments()[0]!;

    [Fact]
    public async Task Deberia_EnviarElReporte_AlBuzonDeOperacion()
    {
        var result = await CreateSut().Submit(Request(), CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();

        var enviado = Captured();
        enviado.ToEmail.Should().Be("operacion@ejemplo.com");
        enviado.Message.Should().Be("No puedo guardar una labor.");
        enviado.ReporterEmail.Should().Be("antonio@ejemplo.com");
    }

    [Fact]
    public async Task Deberia_LeerElNavegadorDeLaCabecera_YNoDelCuerpo()
    {
        // El servidor ya tiene ese dato; pedírselo al cliente solo añadiría un campo falseable.
        await CreateSut().Submit(Request(), CancellationToken.None);

        Captured().Context.UserAgent.Should().Be(UserAgent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Deberia_ExigirTexto(string? message)
    {
        var result = await CreateSut().Submit(Request(message: message), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ApiErrorResponse>()
            .Which.Error.Code.Should().Be(ErrorCodes.ValidationRequiredFeedbackMessage);
    }

    [Fact]
    public async Task Deberia_RechazarUnTipoFueraDelCatalogo()
    {
        var result = await CreateSut().Submit(Request(kind: "queja"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ApiErrorResponse>()
            .Which.Error.Code.Should().Be(ErrorCodes.ValidationFeedbackKindInvalid);
    }

    [Fact]
    public async Task Deberia_RechazarUnMensajeDesmesurado()
    {
        var largo = new string('a', FeedbackController.MaxMessageLength + 1);

        var result = await CreateSut().Submit(Request(message: largo), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ApiErrorResponse>()
            .Which.Error.Code.Should().Be(ErrorCodes.ValidationFeedbackMessageLength);
    }

    [Theory]
    // Los filtros del panel viajan en la URL desde MVP-403 y llevan identificadores de terreno: la
    // query se corta aquí, no solo en el cliente.
    [InlineData("/app/vision-general?plot_ids=6f1b&season=2026", "/app/vision-general")]
    [InlineData("/app/diario#detalle", "/app/diario")]
    // Lo que no tiene forma de ruta del producto no llega al correo.
    [InlineData("https://otro-sitio.example/app", null)]
    [InlineData("/app/<script>", null)]
    public async Task Deberia_QuedarseSoloConLaRuta(string enviada, string? esperada)
    {
        await CreateSut().Submit(Request(path: enviada), CancellationToken.None);

        Captured().Context.Path.Should().Be(esperada);
    }

    [Theory]
    [InlineData("3f8c1d9a4b2e4f6a8c0d2e", "3f8c1d9a4b2e4f6a8c0d2e")]
    // Un identificador que no puede haber emitido `RequestIdMiddleware` no sirve para buscar en la
    // traza: se descarta en vez de mandar a quien lo lea a buscar algo que no existe.
    [InlineData("no es un id", null)]
    [InlineData("", null)]
    public async Task Deberia_AceptarLaCorrelacion_SoloSiTieneLaFormaQueEmiteElServidor(
        string requestId,
        string? esperado)
    {
        await CreateSut().Submit(Request(requestId: requestId), CancellationToken.None);

        Captured().Context.LastFailedRequestId.Should().Be(esperado);
    }

    [Fact]
    public async Task Deberia_NegarseTrasAgotarElCupo_DiciendoCuantoFalta()
    {
        var sut = CreateSut();

        for (var envio = 0; envio < FeedbackRateLimiter.MaxPerWindow; envio++)
            (await sut.Submit(Request(), CancellationToken.None)).Should().BeOfType<AcceptedResult>();

        var result = await sut.Submit(Request(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        sut.Response.Headers.RetryAfter.ToString().Should().NotBeNullOrEmpty();
        await _sender.Received(FeedbackRateLimiter.MaxPerWindow)
            .SendAsync(Arg.Any<FeedbackEmail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_DecirQueElCanalNoEstaDisponible_Cuando_FaltaElBuzon()
    {
        var result = await CreateSut(recipient: string.Empty).Submit(Request(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        await _sender.DidNotReceive().SendAsync(Arg.Any<FeedbackEmail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoConfirmarNada_Cuando_ElEnvioFalla()
    {
        _sender.SendAsync(Arg.Any<FeedbackEmail>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("SMTP caído")));

        var sut = CreateSut();
        var result = await sut.Submit(Request(), CancellationToken.None);

        // CA-3: un fallo se cuenta. Decir «enviado» sin haber enviado es peor que el propio fallo.
        var error = result.Should().BeOfType<ObjectResult>().Subject;
        error.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        error.Value.Should().BeOfType<ApiErrorResponse>()
            .Which.Error.Code.Should().Be(ErrorCodes.FeedbackDeliveryFailed);

        // Y no gasta cupo: sería castigar a quien lo intentó por un fallo nuestro.
        _limiter.IsAllowed(UserId, out _).Should().BeTrue();
    }
}
