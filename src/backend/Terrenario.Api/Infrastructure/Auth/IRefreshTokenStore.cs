namespace Terrenario.Api.Infrastructure.Auth;

public interface IRefreshTokenStore
{
    Task<string> CreateAsync(Guid userId, CancellationToken ct = default);
    Task<Guid> ValidateAndRotateAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// MVP-505 (CA-3) — Revoca **todas** las sesiones vivas de una cuenta. Lo usa la baja de cuenta:
    /// sin esto, un token de refresco emitido antes seguiría sirviendo para volver a entrar.
    /// Devuelve cuántas se revocaron, que es evidencia para el registro de la operación.
    /// </summary>
    Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Sesiones vivas de una cuenta, para que la confirmacion de baja diga cuantas se cerraran.</summary>
    Task<int> CountActiveForUserAsync(Guid userId, CancellationToken ct = default);
}
