namespace Terrenario.Api.Domain.Seasons;

public sealed class SeasonValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
