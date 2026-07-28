using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de dominio de la solicitud de traspaso y reactivación (MVP-206, CA-10): el enlace es de un
/// solo uso, tiene caducidad, solo sirve a su destinatario y solo lo resuelve quien dio de baja.
/// </summary>
public class WorkspaceReactivationRequestTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid RecipientId = Guid.NewGuid();
    private static readonly Guid AuthorizerId = Guid.NewGuid();

    private static WorkspaceReactivationRequest Issue(TimeSpan? lifetime = null)
        => WorkspaceReactivationRequest.Issue(
            WorkspaceId, RecipientId, AuthorizerId, "hash", lifetime ?? TimeSpan.FromDays(7));

    [Fact]
    public void Issue_Deberia_NacerPendienteYConCaducidad()
    {
        var request = Issue();

        request.Status.Should().Be(ReactivationRequestStatuses.Pending);
        request.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        request.RequestedAt.Should().BeNull();
    }

    [Fact]
    public void Submit_Deberia_DejarlaEsperandoAutorizacion()
    {
        var request = Issue();
        var moment = DateTimeOffset.UtcNow;

        request.Submit(RecipientId, moment);

        request.Status.Should().Be(ReactivationRequestStatuses.Requested);
        request.RequestedAt.Should().Be(moment);
    }

    [Fact]
    public void Submit_Deberia_Rechazar_ATerceros()
    {
        var request = Issue();

        var act = () => request.Submit(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ReactivationRequestNotFound);
    }

    [Fact]
    public void Submit_Deberia_Rechazar_UnSegundoUso()
    {
        // CA-10 — el enlace es de un solo uso.
        var request = Issue();
        request.Submit(RecipientId, DateTimeOffset.UtcNow);

        var act = () => request.Submit(RecipientId, DateTimeOffset.UtcNow);

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleReactivationAlreadyUsed);
    }

    [Fact]
    public void Submit_Deberia_Rechazar_UnEnlaceCaducado()
    {
        var request = Issue(TimeSpan.FromSeconds(1));

        var act = () => request.Submit(RecipientId, DateTimeOffset.UtcNow.AddMinutes(5));

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleReactivationExpired);
    }

    [Fact]
    public void Authorize_Deberia_ExigirQueLaSolicitudSeHayaPedido()
    {
        var request = Issue();

        var act = () => request.Authorize(AuthorizerId, DateTimeOffset.UtcNow);

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleReactivationNotRequested);
    }

    [Fact]
    public void Authorize_Deberia_Rechazar_ACualquieraQueNoDioDeBaja()
    {
        // CA-10 — nadie más puede reactivar el Workspace por esta vía.
        var request = Issue();
        request.Submit(RecipientId, DateTimeOffset.UtcNow);

        var act = () => request.Authorize(RecipientId, DateTimeOffset.UtcNow);

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ReactivationRequestNotFound);
    }

    [Fact]
    public void AuthorizeYDeny_Deberian_ResolverLaSolicitud()
    {
        var authorized = Issue();
        authorized.Submit(RecipientId, DateTimeOffset.UtcNow);
        authorized.Authorize(AuthorizerId, DateTimeOffset.UtcNow);
        authorized.Status.Should().Be(ReactivationRequestStatuses.Authorized);
        authorized.ResolvedAt.Should().NotBeNull();

        var denied = Issue();
        denied.Submit(RecipientId, DateTimeOffset.UtcNow);
        denied.Deny(AuthorizerId, DateTimeOffset.UtcNow);
        denied.Status.Should().Be(ReactivationRequestStatuses.Denied);
    }

    [Fact]
    public void Close_Deberia_InvalidarSoloLosEnlacesVivos()
    {
        var open = Issue();
        open.Close(DateTimeOffset.UtcNow);
        open.Status.Should().Be(ReactivationRequestStatuses.Closed);

        var resolved = Issue();
        resolved.Submit(RecipientId, DateTimeOffset.UtcNow);
        resolved.Deny(AuthorizerId, DateTimeOffset.UtcNow);
        resolved.Close(DateTimeOffset.UtcNow);
        resolved.Status.Should().Be(ReactivationRequestStatuses.Denied);
    }
}
