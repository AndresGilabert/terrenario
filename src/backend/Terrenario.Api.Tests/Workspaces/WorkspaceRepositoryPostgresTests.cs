using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests del repositorio contra PostgreSQL real (no el proveedor InMemory): así la consulta pasa por la
/// traducción a SQL de EF Core y se detectan errores "could not be translated" que los mocks no ven.
/// Regresión de MVP-107: <see cref="WorkspaceRepository.ListActiveMembershipsAsync"/> ordenaba por
/// una propiedad del DTO proyectado y reventaba con HTTP 500 en todo el listado de Workspaces.
/// </summary>
public sealed class WorkspaceRepositoryPostgresTests : RepositoryTestBase
{

    [Fact]
    public async Task ListActiveMembershipsAsync_Deberia_DevolverTodas_OrdenadasPorNombre()
    {
        // Arrange — un usuario con membresía activa en dos Workspaces
        var user = User.Create("google-sub", "Andrés", "andres@ejemplo.com");
        Db.Users.Add(user);

        var zeta = Workspace.Create(user.Id, "Zeta");
        var alpha = Workspace.Create(user.Id, "Alpha");
        Db.Workspaces.AddRange(zeta, alpha);
        Db.WorkspaceMembers.Add(WorkspaceMember.CreateMember(zeta.Id, user.Id));
        Db.WorkspaceMembers.Add(WorkspaceMember.CreateMember(alpha.Id, user.Id));
        await Db.SaveChangesAsync();

        var repository = new WorkspaceRepository(Db);

        // Act — antes del fix esto lanzaba InvalidOperationException al traducir el OrderBy del DTO
        var memberships = await repository.ListActiveMembershipsAsync(user.Id);

        // Assert — ambas membresías, ordenadas por nombre de Workspace
        memberships.Select(m => m.Name).Should().Equal("Alpha", "Zeta");
    }

}
