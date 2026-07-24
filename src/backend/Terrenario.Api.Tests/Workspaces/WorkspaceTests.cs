using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

public class WorkspaceTests
{
    [Fact]
    public void Deberia_CrearWorkspace_Cuando_DatosSonValidos()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var workspace = Workspace.Create(ownerId, "Finca El Olivar");

        // Assert
        workspace.Id.Should().NotBeEmpty();
        workspace.OwnerId.Should().Be(ownerId);
        workspace.Name.Should().Be("Finca El Olivar");
        workspace.CreatedAt.Should().Be(workspace.UpdatedAt);
    }

    [Fact]
    public void Deberia_NormalizarNombre_Cuando_LlegaConEspaciosSobrantes()
    {
        // Act
        var workspace = Workspace.Create(Guid.NewGuid(), "   Finca El Olivar   ");

        // Assert
        workspace.Name.Should().Be("Finca El Olivar");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deberia_Rechazar_Cuando_NombreEstaVacio(string nombre)
    {
        // Act
        var act = () => Workspace.Create(Guid.NewGuid(), nombre);

        // Assert
        act.Should().Throw<WorkspaceValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredWorkspaceName);
    }

    [Fact]
    public void Deberia_Rechazar_Cuando_NombreSuperaLongitudMaxima()
    {
        // Arrange
        var nombreDemasiadoLargo = new string('a', Workspace.NameMaxLength + 1);

        // Act
        var act = () => Workspace.Create(Guid.NewGuid(), nombreDemasiadoLargo);

        // Assert
        act.Should().Throw<WorkspaceValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationWorkspaceNameLength);
    }

    [Fact]
    public void Deberia_Rechazar_Cuando_PropietarioNoEsValido()
    {
        // Act
        var act = () => Workspace.Create(Guid.Empty, "Finca El Olivar");

        // Assert
        act.Should().Throw<WorkspaceValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredWorkspaceOwner);
    }

    [Fact]
    public void Deberia_CrearMembresiaActivaDelPropietario_Cuando_SeCreaElWorkspace()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(ownerId, "Finca El Olivar");

        // Act
        var membership = workspace.CreateOwnerMembership();

        // Assert
        membership.WorkspaceId.Should().Be(workspace.Id);
        membership.UserId.Should().Be(ownerId);
        membership.Role.Should().Be(WorkspaceRoles.Owner);
        membership.Active.Should().BeTrue();
    }
}
