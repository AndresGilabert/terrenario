using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests contra PostgreSQL real de las consultas que sostienen la administración de miembros (MVP-204):
/// el listado de personas (join <c>workspace_members</c> × <c>users</c> ordenado por columna real,
/// lección de P-014), los contadores de la invariante CA-8 y las invitaciones por email pendientes.
/// </summary>
public sealed class WorkspaceMembersRepositoryPostgresTests : RepositoryTestBase
{

    private async Task<User> SeedUserAsync(string suffix, string displayName)
    {
        var user = User.Create($"google-sub{suffix}", displayName, $"user{suffix}@ejemplo.com");
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task ListMembersAsync_Deberia_DevolverActivosYRevocados_ConNombreDeCuenta_OrdenadosPorNombre()
    {
        // Arrange — propietario activo (Zoe), miembro activo (Ana), miembro revocado (Bruno)
        var owner = await SeedUserAsync("-owner", "Zoe");
        var active = await SeedUserAsync("-active", "Ana");
        var revoked = await SeedUserAsync("-revoked", "Bruno");
        var workspace = Workspace.Create(owner.Id, "Finca");
        Db.Workspaces.Add(workspace);

        Db.WorkspaceMembers.Add(WorkspaceMember.CreateOwner(workspace.Id, owner.Id));
        Db.WorkspaceMembers.Add(WorkspaceMember.CreateMember(workspace.Id, active.Id));
        var revokedMember = WorkspaceMember.CreateMember(workspace.Id, revoked.Id);
        revokedMember.Revoke();
        Db.WorkspaceMembers.Add(revokedMember);
        await Db.SaveChangesAsync();

        var repository = new WorkspaceRepository(Db);

        // Act — antes fallaría si el OrderBy fuese sobre el DTO proyectado (P-014)
        var members = await repository.ListMembersAsync(workspace.Id);

        // Assert — todas las membresías (activo + revocado), con email/nombre y orden por nombre real
        members.Select(m => m.DisplayName).Should().Equal("Ana", "Bruno", "Zoe");
        members.Single(m => m.DisplayName == "Bruno").Status.Should().Be(WorkspaceMemberStatuses.Revoked);
        members.Single(m => m.DisplayName == "Zoe").Role.Should().Be(WorkspaceRoles.Owner);
        members.Single(m => m.DisplayName == "Ana").Email.Should().Be("user-active@ejemplo.com");
    }

    [Fact]
    public async Task Counters_Deberia_ContarSoloActivos_Y_SoloPropietariosActivos()
    {
        var owner = await SeedUserAsync("-o", "Zoe");
        var active = await SeedUserAsync("-a", "Ana");
        var revoked = await SeedUserAsync("-r", "Bruno");
        var workspace = Workspace.Create(owner.Id, "Finca");
        Db.Workspaces.Add(workspace);

        Db.WorkspaceMembers.Add(WorkspaceMember.CreateOwner(workspace.Id, owner.Id));
        Db.WorkspaceMembers.Add(WorkspaceMember.CreateMember(workspace.Id, active.Id));
        var revokedMember = WorkspaceMember.CreateMember(workspace.Id, revoked.Id);
        revokedMember.Revoke();
        Db.WorkspaceMembers.Add(revokedMember);
        await Db.SaveChangesAsync();

        var repository = new WorkspaceRepository(Db);

        (await repository.CountActiveMembersAsync(workspace.Id)).Should().Be(2);
        (await repository.CountActiveOwnersAsync(workspace.Id)).Should().Be(1);

        var found = await repository.FindActiveMemberAsync(workspace.Id, active.Id);
        found.Should().NotBeNull();
        found!.UserId.Should().Be(active.Id);
    }

    /// <summary>
    /// MVP-208 (CA-7) — La superficie de personas y accesos pendientes proyecta los <b>dos</b> canales:
    /// el enlace compartible también es un acceso vivo que hay que poder retirar (hallazgo R-15). Lo
    /// que sigue fuera es todo lo que ya no está pendiente.
    /// </summary>
    [Fact]
    public async Task ListPendingAsync_Deberia_IncluirEnlace_Y_ExcluirNoPendientes()
    {
        var owner = await SeedUserAsync("-o2", "Zoe");
        var acceptor = await SeedUserAsync("-acc", "Otro");
        var workspace = Workspace.Create(owner.Id, "Finca");
        Db.Workspaces.Add(workspace);
        await Db.SaveChangesAsync();

        var pendingEmail = WorkspaceInvitation.Create(
            workspace.Id, owner.Id, InvitationChannels.Email, "invitado@ejemplo.com", "hash-e", TimeSpan.FromDays(7));
        var link = WorkspaceInvitation.Create(
            workspace.Id, owner.Id, InvitationChannels.Link, null, "hash-l", TimeSpan.FromDays(7));
        var accepted = WorkspaceInvitation.Create(
            workspace.Id, owner.Id, InvitationChannels.Email, "otro@ejemplo.com", "hash-a", TimeSpan.FromDays(7));
        accepted.Accept(acceptor.Id, "otro@ejemplo.com", DateTimeOffset.UtcNow);
        Db.WorkspaceInvitations.AddRange(pendingEmail, link, accepted);
        await Db.SaveChangesAsync();

        var repository = new WorkspaceInvitationRepository(Db);

        var invited = await repository.ListPendingAsync(workspace.Id);

        invited.Should().HaveCount(2);
        invited.Should().ContainSingle(i => i.Channel == InvitationChannels.Email)
            .Which.Email.Should().Be("invitado@ejemplo.com");
        invited.Should().ContainSingle(i => i.Channel == InvitationChannels.Link)
            .Which.Email.Should().BeNull();
    }

}
