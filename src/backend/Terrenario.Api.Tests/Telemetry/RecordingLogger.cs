using Microsoft.Extensions.Logging;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-601 — Logger que se queda con las propiedades estructuradas de cada renglón emitido.
///
/// Hace falta uno propio porque lo que hay que comprobar de la telemetría **no es el texto**: es qué
/// dimensiones salen y, sobre todo, qué dimensiones <b>no</b> salen (CA-3, sin PII). Un
/// <c>NullLogger</c> no permite afirmar ninguna de las dos cosas.
/// </summary>
public sealed class RecordingLogger<T> : ILogger<T>
{
    public List<IReadOnlyDictionary<string, object?>> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object?>> properties)
            Entries.Add(properties.ToDictionary(p => p.Key, p => p.Value));
    }

    /// <summary>Nombres de las dimensiones del último evento, sin la plantilla del mensaje.</summary>
    public IReadOnlyCollection<string> LastDimensions() =>
        Entries[^1].Keys.Where(k => k != "{OriginalFormat}").ToArray();

    public IReadOnlyDictionary<string, object?> Last() => Entries[^1];
}
