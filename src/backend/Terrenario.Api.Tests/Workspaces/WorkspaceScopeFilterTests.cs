using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

public class WorkspaceScopeFilterTests
{
    private readonly IActiveWorkspaceResolver _resolver = Substitute.For<IActiveWorkspaceResolver>();
    private readonly WorkspaceScopeContext _context = new();

    private WorkspaceScopeFilter CreateSut() => new(_resolver, _context);

    private static ActionExecutingContext BuildExecutingContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    private static ClaimsPrincipal UserWith(Guid? userId)
    {
        var claims = userId is null ? [] : new[] { new Claim("sub", userId.Value.ToString()) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static (ActionExecutionDelegate next, Func<bool> wasCalled) TrackNext(ActionExecutingContext ctx)
    {
        var called = false;
        ActionExecutionDelegate next = () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), controller: new object()));
        };
        return (next, () => called);
    }

    [Fact]
    public async Task Deberia_Devolver401_Cuando_LaSesionNoIdentificaUsuario()
    {
        var ctx = BuildExecutingContext(UserWith(null));
        var (next, wasCalled) = TrackNext(ctx);

        await CreateSut().OnActionExecutionAsync(ctx, next);

        ctx.Result.Should().BeOfType<UnauthorizedObjectResult>();
        wasCalled().Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_Devolver403ScopeRequired_Cuando_ElUsuarioNoTieneWorkspaceActivo()
    {
        var userId = Guid.NewGuid();
        _resolver.ResolveAsync(userId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((WorkspaceSummary?)null);

        var ctx = BuildExecutingContext(UserWith(userId));
        var (next, wasCalled) = TrackNext(ctx);

        await CreateSut().OnActionExecutionAsync(ctx, next);

        var result = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.Value.Should().BeOfType<ApiErrorResponse>()
            .Which.Error.Code.Should().Be(ErrorCodes.AuthWorkspaceScopeRequired);
        wasCalled().Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_PoblarContextoYContinuar_Cuando_HayWorkspaceActivo()
    {
        var userId = Guid.NewGuid();
        var workspace = new WorkspaceSummary(Guid.NewGuid(), "Finca El Olivar");
        _resolver.ResolveAsync(userId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(workspace);

        var ctx = BuildExecutingContext(UserWith(userId));
        var (next, wasCalled) = TrackNext(ctx);

        await CreateSut().OnActionExecutionAsync(ctx, next);

        wasCalled().Should().BeTrue();
        ctx.Result.Should().BeNull();
        _context.HasWorkspace.Should().BeTrue();
        _context.WorkspaceId.Should().Be(workspace.Id);
    }
}
