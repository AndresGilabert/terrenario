using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

public class WorkspaceAccessExceptionFilterTests
{
    private static ExceptionContext BuildContext(Exception exception)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ControllerActionDescriptor());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    [Fact]
    public void Deberia_Mapear403Forbidden_Cuando_ElRecursoEsDeOtroWorkspace()
    {
        var ctx = BuildContext(new WorkspaceAccessDeniedException("El recurso no pertenece a tu Workspace activo."));

        new WorkspaceAccessExceptionFilter().OnException(ctx);

        ctx.ExceptionHandled.Should().BeTrue();
        var result = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.Value.Should().BeOfType<ApiErrorResponse>()
            .Which.Error.Code.Should().Be(ErrorCodes.AuthWorkspaceForbidden);
    }

    [Fact]
    public void Deberia_IgnorarOtrasExcepciones()
    {
        var ctx = BuildContext(new InvalidOperationException("otra cosa"));

        new WorkspaceAccessExceptionFilter().OnException(ctx);

        ctx.ExceptionHandled.Should().BeFalse();
        ctx.Result.Should().BeNull();
    }
}
