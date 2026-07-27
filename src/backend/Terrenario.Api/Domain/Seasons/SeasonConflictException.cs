namespace Terrenario.Api.Domain.Seasons;

/// <summary>
/// El Workspace ya tiene una temporada activa. En MVP-201 solo se crea la primera (RN-022); la
/// gestión de varias temporadas y el cambio de activa son alcance de MVP-203.
/// </summary>
public sealed class SeasonConflictException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
