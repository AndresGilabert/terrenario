using System.Reflection;

namespace Terrenario.Api.Common;

/// <summary>
/// MVP-711 — Qué versión está sirviendo esta instancia.
///
/// El dato lo pone el pipeline de publicación (<c>deploy.yml</c>) al compilar, con el <b>tag</b> que
/// disparó el despliegue: <c>dotnet publish -p:InformationalVersion=v0.6.0-hito-f</c>. Sin ese
/// parámetro —una compilación local— el SDK deja el <c>1.0.0</c> por defecto, que es información
/// honesta: dice «esto no viene de una publicación».
///
/// <b>Lo sabe el servidor, no el cliente</b>, y esa es la decisión que importa. La API y el cliente
/// se publican como un único artefacto —la API sirve el estático—, así que la versión del ensamblado
/// <i>es</i> la versión desplegada de las dos mitades. Preguntársela al navegador habría añadido un
/// dato que quien reporta puede falsear sin querer (una pestaña abierta desde antes del último
/// despliegue) y que además obligaría a incrustar la versión en el bundle.
/// </summary>
public static class DeployedVersion
{
    /// <summary>Se resuelve una vez: es un atributo del ensamblado y no cambia en caliente.</summary>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var informational = typeof(DeployedVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return string.IsNullOrWhiteSpace(informational) ? "desconocida" : informational;
    }
}
