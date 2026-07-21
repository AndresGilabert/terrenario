namespace Terrenario.Api.Domain.Users;

public sealed class User
{
    public Guid Id { get; private set; }
    public string GoogleSub { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User() { }

    public static User Create(string googleSub, string displayName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleSub);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new User
        {
            Id = Guid.NewGuid(),
            GoogleSub = googleSub,
            DisplayName = displayName,
            Email = email,
            Active = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateProfile(string displayName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        DisplayName = displayName;
        Email = email;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
