using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de dominio del ciclo de vida del Workspace (MVP-206): renombrado con las validaciones del
/// alta (CA-1), baja lógica que nunca borra (CA-2), reactivación (CA-7) y traspaso de propiedad
/// (CA-4/CA-5).
/// </summary>
public class WorkspaceLifecycleTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static Workspace CreateWorkspace() => Workspace.Create(OwnerId, "Finca Vieja");

    [Fact]
    public void Rename_Deberia_NormalizarYActualizar()
    {
        var workspace = CreateWorkspace();

        workspace.Rename("  Finca Nueva  ");

        workspace.Name.Should().Be("Finca Nueva");
    }

    [Fact]
    public void Rename_Deberia_Rechazar_NombreVacio()
    {
        var workspace = CreateWorkspace();

        var act = () => workspace.Rename("   ");

        act.Should().Throw<WorkspaceValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredWorkspaceName);
    }

    [Fact]
    public void Rename_Deberia_Rechazar_NombreDemasiadoLargo()
    {
        var workspace = CreateWorkspace();

        var act = () => workspace.Rename(new string('a', Workspace.NameMaxLength + 1));

        act.Should().Throw<WorkspaceValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationWorkspaceNameLength);
    }

    [Fact]
    public void SoftDelete_Deberia_MarcarLaBajaSinPerderDatos()
    {
        // CA-2 — la baja es lógica: el agregado sigue existiendo con su nombre y su propietario.
        var workspace = CreateWorkspace();
        var moment = DateTimeOffset.UtcNow;

        workspace.SoftDelete(OwnerId, moment);

        workspace.IsDeleted.Should().BeTrue();
        workspace.DeletedAt.Should().Be(moment);
        workspace.DeletedByUserId.Should().Be(OwnerId);
        workspace.Name.Should().Be("Finca Vieja");
    }

    [Fact]
    public void SoftDelete_Deberia_Rechazar_UnaSegundaBaja()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(OwnerId, DateTimeOffset.UtcNow);

        var act = () => workspace.SoftDelete(OwnerId, DateTimeOffset.UtcNow);

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkspaceDeleted);
    }

    [Fact]
    public void Rename_Deberia_Rechazar_SobreUnWorkspaceDadoDeBaja()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(OwnerId, DateTimeOffset.UtcNow);

        var act = () => workspace.Rename("Otro nombre");

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkspaceDeleted);
    }

    [Fact]
    public void Reactivate_Deberia_DevolverElWorkspaceAlaVida()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(OwnerId, DateTimeOffset.UtcNow);

        workspace.Reactivate();

        workspace.IsDeleted.Should().BeFalse();
        workspace.DeletedByUserId.Should().BeNull();
    }

    [Fact]
    public void Reactivate_Deberia_Rechazar_SiNoEstabaDadoDeBaja()
    {
        var workspace = CreateWorkspace();

        var act = workspace.Reactivate;

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkspaceNotDeleted);
    }

    [Fact]
    public void TransferOwnershipTo_Deberia_CambiarElPropietario()
    {
        var workspace = CreateWorkspace();
        var newOwnerId = Guid.NewGuid();

        workspace.TransferOwnershipTo(newOwnerId);

        workspace.OwnerId.Should().Be(newOwnerId);
    }

    [Fact]
    public void TransferOwnershipTo_Deberia_Rechazar_AlPropietarioActual()
    {
        var workspace = CreateWorkspace();

        var act = () => workspace.TransferOwnershipTo(OwnerId);

        act.Should().Throw<WorkspaceMemberException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleOwnershipTransferToSelf);
    }

    [Fact]
    public void PromoteYDemote_Deberian_GobernarElRolDeLaMembresia()
    {
        var workspaceId = Guid.NewGuid();
        var member = WorkspaceMember.CreateMember(workspaceId, Guid.NewGuid());

        member.PromoteToOwner();
        member.Role.Should().Be(WorkspaceRoles.Owner);

        member.DemoteToMember();
        member.Role.Should().Be(WorkspaceRoles.Member);
        // Degradar no retira el acceso: sigue siendo miembro activo (RN-034, permisos planos).
        member.IsActive.Should().BeTrue();
    }
}
