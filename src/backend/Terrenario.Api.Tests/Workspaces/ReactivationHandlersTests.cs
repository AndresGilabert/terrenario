using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Tokens;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de la vuelta de un Workspace dado de baja (MVP-206, HU-5/HU-6): solicitar por enlace de un
/// solo uso (CA-10) y autorizar/denegar por parte de quien lo dio de baja (CA-7). Al autorizar, el
/// Workspace se reactiva y la propiedad pasa al solicitante en la misma operación.
/// </summary>
public class ReactivationHandlersTests
{
    private readonly IWorkspaceReactivationRequestRepository _reactivationRepository =
        Substitute.For<IWorkspaceReactivationRequestRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IOneTimeTokenService _tokenService = Substitute.For<IOneTimeTokenService>();
    private readonly IWorkspaceLifecycleEmailSender _emailSender =
        Substitute.For<IWorkspaceLifecycleEmailSender>();

    private static readonly Guid AuthorizerId = Guid.NewGuid();
    private static readonly Guid RecipientId = Guid.NewGuid();

    private readonly Workspace _workspace = Workspace.Create(AuthorizerId, "Finca El Olivar");

    public ReactivationHandlersTests()
    {
        _workspace.SoftDelete(AuthorizerId, DateTimeOffset.UtcNow);
        _tokenService.Hash("token").Returns("hash");
        _emailSender.IsEnabled.Returns(true);
        _workspaceRepository.FindIncludingDeletedAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(_workspace);
        _reactivationRepository.ListOpenForWorkspaceAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceReactivationRequest>());
    }

    private WorkspaceReactivationRequest IssuedRequest() => WorkspaceReactivationRequest.Issue(
        _workspace.Id, RecipientId, AuthorizerId, "hash", TimeSpan.FromDays(7));

    private RequestReactivationHandler CreateRequestSut(WorkspaceReactivationRequest? request)
    {
        _reactivationRepository.FindByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(request);
        _userRepository.FindByIdAsync(AuthorizerId, Arg.Any<CancellationToken>())
            .Returns(User.Create("sub-a", "Antonio", "antonio@ejemplo.com"));
        _userRepository.FindByIdAsync(RecipientId, Arg.Any<CancellationToken>())
            .Returns(User.Create("sub-l", "Lucia", "lucia@ejemplo.com"));

        return new RequestReactivationHandler(
            _reactivationRepository,
            _workspaceRepository,
            _userRepository,
            _tokenService,
            _emailSender,
            Options.Create(new WorkspaceLifecycleOptions()),
            Substitute.For<ILogger<RequestReactivationHandler>>());
    }

    private ResolveReactivationHandler CreateResolveSut(WorkspaceReactivationRequest request)
    {
        _reactivationRepository.FindByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        return new ResolveReactivationHandler(_reactivationRepository, _workspaceRepository);
    }

    [Fact]
    public async Task Solicitar_Deberia_ConsumirElEnlaceYAvisarAQuienDioDeBaja()
    {
        var request = IssuedRequest();
        var sut = CreateRequestSut(request);

        var result = await sut.HandleAsync("token", RecipientId);

        request.Status.Should().Be(ReactivationRequestStatuses.Requested);
        result.CanRequest.Should().BeFalse();
        await _emailSender.Received(1).SendReactivationRequestedAsync(
            Arg.Is<ReactivationRequestedEmail>(m => m.ToEmail == "antonio@ejemplo.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Solicitar_Deberia_OcultarElEnlaceDeOtraPersona()
    {
        var sut = CreateRequestSut(IssuedRequest());

        var act = async () => await sut.HandleAsync("token", Guid.NewGuid());

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ReactivationRequestNotFound);
    }

    [Fact]
    public async Task Solicitar_Deberia_Rechazar_SiElWorkspaceYaVolvio()
    {
        _workspace.Reactivate();
        var sut = CreateRequestSut(IssuedRequest());

        var act = async () => await sut.HandleAsync("token", RecipientId);

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkspaceNotDeleted);
    }

    [Fact]
    public async Task Autorizar_Deberia_ReactivarYTraspasarAlSolicitante()
    {
        // CA-7 — el Workspace vuelve y la propiedad pasa a quien lo pidió, sin instante sin dueño.
        var request = IssuedRequest();
        request.Submit(RecipientId, DateTimeOffset.UtcNow);
        var newOwner = WorkspaceMember.CreateMember(_workspace.Id, RecipientId);
        var previousOwner = WorkspaceMember.CreateOwner(_workspace.Id, AuthorizerId);
        _workspaceRepository.FindActiveMemberAsync(_workspace.Id, RecipientId, Arg.Any<CancellationToken>())
            .Returns(newOwner);
        _workspaceRepository.FindActiveMemberAsync(_workspace.Id, AuthorizerId, Arg.Any<CancellationToken>())
            .Returns(previousOwner);
        var sut = CreateResolveSut(request);

        var outcome = await sut.AuthorizeAsync(request.Id, AuthorizerId);

        outcome.NewOwnerUserId.Should().Be(RecipientId);
        _workspace.IsDeleted.Should().BeFalse();
        _workspace.OwnerId.Should().Be(RecipientId);
        newOwner.Role.Should().Be(WorkspaceRoles.Owner);
        previousOwner.Role.Should().Be(WorkspaceRoles.Member);
        previousOwner.Status.Should().Be(WorkspaceMemberStatuses.Active);
        request.Status.Should().Be(ReactivationRequestStatuses.Authorized);
    }

    [Fact]
    public async Task Autorizar_Deberia_CerrarLosDemasEnlacesDelWorkspace()
    {
        // CA-10 — un enlace antiguo no puede encadenar una segunda reactivación.
        var request = IssuedRequest();
        request.Submit(RecipientId, DateTimeOffset.UtcNow);
        var other = WorkspaceReactivationRequest.Issue(
            _workspace.Id, Guid.NewGuid(), AuthorizerId, "otro-hash", TimeSpan.FromDays(7));
        _reactivationRepository.ListOpenForWorkspaceAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceReactivationRequest> { request, other });
        _workspaceRepository.FindActiveMemberAsync(_workspace.Id, RecipientId, Arg.Any<CancellationToken>())
            .Returns(WorkspaceMember.CreateMember(_workspace.Id, RecipientId));
        var sut = CreateResolveSut(request);

        await sut.AuthorizeAsync(request.Id, AuthorizerId);

        other.Status.Should().Be(ReactivationRequestStatuses.Closed);
        request.Status.Should().Be(ReactivationRequestStatuses.Authorized);
    }

    [Fact]
    public async Task Autorizar_Deberia_Rechazar_ACualquierOtraCuenta()
    {
        var request = IssuedRequest();
        request.Submit(RecipientId, DateTimeOffset.UtcNow);
        var sut = CreateResolveSut(request);

        var act = async () => await sut.AuthorizeAsync(request.Id, Guid.NewGuid());

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ReactivationRequestNotFound);
        _workspace.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Denegar_Deberia_DejarElWorkspaceDadoDeBaja()
    {
        var request = IssuedRequest();
        request.Submit(RecipientId, DateTimeOffset.UtcNow);
        var sut = CreateResolveSut(request);

        await sut.DenyAsync(request.Id, AuthorizerId);

        request.Status.Should().Be(ReactivationRequestStatuses.Denied);
        _workspace.IsDeleted.Should().BeTrue();
    }
}
