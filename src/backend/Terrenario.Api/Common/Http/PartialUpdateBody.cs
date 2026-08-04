using System.Text.Json;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// El cuerpo del cliente no es JSON válido o no se puede interpretar. Es culpa de quien llama, no del
/// servidor: el borde de transporte la traduce a <c>400</c> (<see cref="InvalidRequestBodyFilter"/>).
/// </summary>
public sealed class InvalidRequestBodyException(string message) : Exception(message);

/// <summary>
/// MVP-502 (<c>P-027</c>) — Lectura de texto de un <see cref="JsonElement"/> <b>con red</b>.
///
/// Es la primitiva que faltaba. <see cref="JsonElement.GetString"/> lanza
/// <see cref="InvalidOperationException"/> («Cannot transcode invalid UTF-8 JSON text») cuando los
/// bytes del cuerpo no son UTF-8 válido, y lo hace <b>después</b> del binding, así que ningún
/// controlador la capturaba y la API respondía <c>500</c> a un error del cliente.
///
/// Toda lectura de texto de un cuerpo de edición parcial pasa por aquí —también las que solo quieren
/// el texto para convertirlo a fecha o a identificador—, que es lo que garantiza que no vuelva a
/// quedarse ninguna suelta.
/// </summary>
public static class JsonText
{
    /// <summary>Texto del elemento, o <c>null</c> si vino explícitamente nulo.</summary>
    public static string? Read(JsonElement element, string key)
    {
        if (element.ValueKind == JsonValueKind.Null) return null;

        // MVP-599 (`R-04`) — `GetString()` lanza `InvalidOperationException` por **dos** motivos, y la
        // primera versión de esto los confundía: además del UTF-8 inválido, falla cuando el valor no
        // es texto (`{"name": 12345}`). Ese caso respondía «el cuerpo debe estar codificado en UTF-8»,
        // que es falso y manda a quien integra a revisar su codificación en vez de su tipo. El código
        // de error era correcto; el mensaje mentía.
        if (element.ValueKind != JsonValueKind.String)
            throw new InvalidRequestBodyException(
                $"El campo '{key}' debe ser un texto.");

        try
        {
            return element.GetString();
        }
        catch (InvalidOperationException)
        {
            throw new InvalidRequestBodyException(
                $"El campo '{key}' no se puede leer: el cuerpo de la petición debe estar codificado en UTF-8.");
        }
    }
}

/// <summary>
/// MVP-502 — Lector común de los cuerpos de <b>edición parcial</b> (<c>PATCH</c>), que en esta API se
/// reciben como <c>[FromBody] Dictionary&lt;string, JsonElement&gt;</c> para poder distinguir «el
/// campo no viene» de «viene vacío» (ver <see cref="FieldUpdate{T}"/>).
///
/// Existe por <c>P-027</c>: los ocho controladores con ese patrón repetían las mismas funciones de
/// lectura, y todas llamaban a <see cref="JsonElement.GetString"/> sin red. Con un cuerpo cuyos bytes
/// no son UTF-8 válido, <c>GetString()</c> lanza <see cref="InvalidOperationException"/>
/// («Cannot transcode invalid UTF-8 JSON text») <b>después</b> del binding, así que nadie la
/// capturaba y la API respondía <c>500</c> a un error del cliente. Además de mentir sobre de quién es
/// la culpa, ensuciaba la observabilidad con errores de servidor que no lo eran.
///
/// Las lecturas de tipo (booleano, entero, decimal) se exponen como <c>Try…</c> a propósito: el
/// <b>código</b> de error de un tipo inválido es de dominio —cada maestro tiene el suyo— y debe
/// seguir decidiéndolo el controlador. Lo que se centraliza aquí es la <b>lectura</b>, no la política.
/// </summary>
public sealed class PartialUpdateBody
{
    private readonly IReadOnlyDictionary<string, JsonElement> _fields;

    private PartialUpdateBody(IReadOnlyDictionary<string, JsonElement> fields) => _fields = fields;

    /// <summary>Un cuerpo ausente equivale a «no cambies nada», no a un error.</summary>
    public static PartialUpdateBody From(Dictionary<string, JsonElement>? body)
        => new(body ?? []);

    public bool Has(string key) => _fields.ContainsKey(key);

    /// <summary>Texto que puede venir vacío pero no nulo en el dominio (p. ej. el nombre).</summary>
    public FieldUpdate<string> ReadString(string key)
        => _fields.TryGetValue(key, out var element)
            ? FieldUpdate<string>.Set(ReadText(element, key))
            : FieldUpdate<string>.Absent;

    /// <summary>Texto opcional: enviarlo a <c>null</c> lo limpia.</summary>
    public FieldUpdate<string?> ReadNullableString(string key)
        => _fields.TryGetValue(key, out var element)
            ? FieldUpdate<string?>.Set(ReadText(element, key))
            : FieldUpdate<string?>.Absent;

    /// <summary><c>false</c> ⇒ el campo vino con un valor que no es booleano.</summary>
    public bool TryReadBool(string key, out FieldUpdate<bool> field)
    {
        field = FieldUpdate<bool>.Absent;
        if (!_fields.TryGetValue(key, out var element)) return true;

        if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;

        field = FieldUpdate<bool>.Set(element.GetBoolean());
        return true;
    }

    /// <summary><c>false</c> ⇒ el campo vino con un valor que no es un entero.</summary>
    public bool TryReadInt(string key, out FieldUpdate<int?> field)
    {
        field = FieldUpdate<int?>.Absent;
        if (!_fields.TryGetValue(key, out var element)) return true;

        if (element.ValueKind == JsonValueKind.Null)
        {
            field = FieldUpdate<int?>.Set(null);
            return true;
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value)) return false;

        field = FieldUpdate<int?>.Set(value);
        return true;
    }

    /// <summary><c>false</c> ⇒ el campo vino con un valor que no es un número.</summary>
    public bool TryReadDecimal(string key, out FieldUpdate<decimal?> field)
    {
        field = FieldUpdate<decimal?>.Absent;
        if (!_fields.TryGetValue(key, out var element)) return true;

        if (element.ValueKind == JsonValueKind.Null)
        {
            field = FieldUpdate<decimal?>.Set(null);
            return true;
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out var value)) return false;

        field = FieldUpdate<decimal?>.Set(value);
        return true;
    }

    private static string? ReadText(JsonElement element, string key) => JsonText.Read(element, key);
}
