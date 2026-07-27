using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Invitations;

/// <summary>
/// Tests de <see cref="WorkspaceInvitation.Reissue"/> (MVP-204, HU-5/CA-6): rotación del token y
/// renovación de la caducidad, con las mismas guardas que la emisión original.
/// </summary>
public class WorkspaceInvitationReissueTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid InviterId = Guid.NewGuid();

    [Fact]
    public void Reissue_Deberia_RotarTokenYRenovarCaducidad_ManteniendoPendiente()
    {
        var invitation = WorkspaceInvitation.Create(
            WorkspaceId, InviterId, InvitationChannels.Email, "vecino@ejemplo.com", "hash-viejo", TimeSpan.FromDays(-1));
        invitation.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeTrue();

        invitation.Reissue("hash-nuevo", TimeSpan.FromDays(7));

        invitation.TokenHash.Should().Be("hash-nuevo");
        invitation.Status.Should().Be(InvitationStatuses.Pending);
        invitation.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Reissue_Deberia_Rechazar_CanalEnlace()
    {
        var link = WorkspaceInvitation.Create(
            WorkspaceId, InviterId, InvitationChannels.Link, null, "hash", TimeSpan.FromDays(7));

        var act = () => link.Reissue("hash-nuevo", TimeSpan.FromDays(7));

        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
    }

    [Fact]
    public void Reissue_Deberia_Rechazar_InvitacionYaAceptada()
    {
        var invitation = WorkspaceInvitation.Create(
            WorkspaceId, InviterId, InvitationChannels.Email, "vecino@ejemplo.com", "hash", TimeSpan.FromDays(7));
        invitation.Accept(Guid.NewGuid(), "vecino@ejemplo.com", DateTimeOffset.UtcNow);

        var act = () => invitation.Reissue("hash-nuevo", TimeSpan.FromDays(7));

        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationAlreadyAccepted);
    }
}
