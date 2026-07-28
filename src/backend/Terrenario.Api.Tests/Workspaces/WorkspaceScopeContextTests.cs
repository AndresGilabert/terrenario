using FluentAssertions;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

public class WorkspaceScopeContextTests
{
    private static WorkspaceScopeContext WithWorkspace(Guid id) =>
        Build(new WorkspaceSummary(id, "Finca El Olivar"));

    private static WorkspaceScopeContext Build(WorkspaceSummary summary)
    {
        var context = new WorkspaceScopeContext();
        context.Set(summary);
        return context;
    }

    [Fact]
    public void HasWorkspace_EsFalse_Cuando_NoSeHaResuelto()
    {
        var context = new WorkspaceScopeContext();

        context.HasWorkspace.Should().BeFalse();
    }

    [Fact]
    public void Workspace_Lanza_Cuando_NoSeHaResuelto()
    {
        var context = new WorkspaceScopeContext();

        var act = () => context.Workspace;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureInScope_NoLanza_Cuando_ElRecursoPerteneceAlWorkspaceActivo()
    {
        var workspaceId = Guid.NewGuid();
        var context = WithWorkspace(workspaceId);

        var act = () => context.EnsureInScope(workspaceId);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureInScope_Lanza_Cuando_ElRecursoEsDeOtroWorkspace()
    {
        var context = WithWorkspace(Guid.NewGuid());

        var act = () => context.EnsureInScope(Guid.NewGuid());

        act.Should().Throw<WorkspaceAccessDeniedException>();
    }
}
