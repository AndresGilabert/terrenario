using FluentAssertions;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

public class WorkspaceMemberTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Deberia_NacerActivaConRolOwner_Cuando_EsMembresiaDelCreador()
    {
        // Act
        var membership = WorkspaceMember.CreateOwner(WorkspaceId, UserId);

        // Assert
        membership.Role.Should().Be(WorkspaceRoles.Owner);
        membership.Status.Should().Be(WorkspaceMemberStatuses.Active);
        membership.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deberia_NacerActivaConRolMember_Cuando_EntraPorInvitacion()
    {
        // Act
        var membership = WorkspaceMember.CreateMember(WorkspaceId, UserId);

        // Assert
        membership.Role.Should().Be(WorkspaceRoles.Member);
        membership.Status.Should().Be(WorkspaceMemberStatuses.Active);
        membership.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deberia_DejarDeDarAcceso_Cuando_SeRevoca()
    {
        // Arrange
        var membership = WorkspaceMember.CreateMember(WorkspaceId, UserId);

        // Act
        membership.Revoke();

        // Assert
        membership.Status.Should().Be(WorkspaceMemberStatuses.Revoked);
        membership.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("invitado", true)]
    [InlineData("activo", true)]
    [InlineData("revocado", true)]
    [InlineData("pendiente", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Deberia_ValidarEstadoContraElCatalogo(string? status, bool esperado)
    {
        // Act / Assert
        WorkspaceMemberStatuses.IsValid(status).Should().Be(esperado);
    }
}
