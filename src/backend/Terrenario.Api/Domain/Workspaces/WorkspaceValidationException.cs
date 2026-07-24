namespace Terrenario.Api.Domain.Workspaces;

public sealed class WorkspaceValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
