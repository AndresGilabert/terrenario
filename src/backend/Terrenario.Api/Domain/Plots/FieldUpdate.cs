namespace Terrenario.Api.Domain.Plots;

/// <summary>
/// Campo de una edición parcial (PATCH). Distingue "no viene en la petición" (<see cref="Present"/>
/// = <c>false</c> ⇒ se mantiene el valor actual) de "viene con un valor" (incluido <c>null</c>/vacío
/// ⇒ se asigna/limpia). Es lo que permite que <c>PATCH /plots/{id}</c> sea de campos parciales
/// (contrato de API) sin que omitir un campo borre datos.
/// </summary>
public readonly record struct FieldUpdate<T>
{
    public bool Present { get; private init; }
    public T? Value { get; private init; }

    public static FieldUpdate<T> Absent => new() { Present = false, Value = default };
    public static FieldUpdate<T> Set(T? value) => new() { Present = true, Value = value };

    /// <summary>Valor a aplicar: el nuevo si vino en la petición, o <paramref name="current"/> si no.</summary>
    public T? Or(T? current) => Present ? Value : current;
}
