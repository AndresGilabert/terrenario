namespace Terrenario.Api.Common.Http;

/// <summary>
/// PLT-101 — Qué dominios alternativos (comprados solo para no perderlos, sin contenido propio)
/// redirigen al dominio canónico del cliente. Vacío por defecto: sin nada configurado, el middleware
/// no hace nada, que es el comportamiento correcto en desarrollo y en cualquier entorno que no sirva
/// detrás de esos dominios.
/// </summary>
public sealed class DomainRedirectOptions
{
    public const string SectionName = "Domains";

    /// <summary>Host al que se redirige, sin esquema (<c>app.terrenario.com</c>).</summary>
    public string CanonicalHost { get; set; } = string.Empty;

    /// <summary>Hosts que deben redirigir a <see cref="CanonicalHost"/>, sin esquema.</summary>
    public IReadOnlyList<string> AlternateHosts { get; set; } = [];
}
