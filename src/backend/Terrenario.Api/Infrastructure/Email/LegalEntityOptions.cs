using System.Reflection;
using System.Text.Json;

namespace Terrenario.Api.Infrastructure.Email;

/// <summary>
/// MVP-715 — Identidad del responsable del tratamiento tal y como tiene que aparecer en el pie de
/// todos los correos del producto (RGPD art. 13, LSSI art. 10).
///
/// <b>Los valores no se escriben aquí.</b> Salen de
/// <c>src/frontend/terrenario-web/src/config/legal-entity.json</c>, el mismo fichero del que se
/// alimentan la Política de Privacidad y los Términos publicados, incrustado en este ensamblado como
/// recurso al compilar (ver <c>Terrenario.Api.csproj</c>). El motivo es el de siempre: dos copias de
/// un dato legal divergen, y la divergencia se descubre cuando la copia equivocada ya está en la
/// bandeja de alguien. Es el mismo criterio con el que <c>SpaContentSecurityPolicy</c> lee la CSP
/// que genera el build del cliente en vez de reescribirla en C#.
///
/// Cada campo se puede sobreescribir por configuración (<c>Legal:*</c>, o <c>Legal__*</c> como
/// variable de entorno) para un despliegue concreto. Un valor en blanco cae al versionado, igual que
/// hace <c>resolveLegalEntity</c> en el cliente: una variable definida y vacía no debe dejar hueco
/// en un texto legal.
/// </summary>
public sealed class LegalEntityOptions
{
    public const string SectionName = "Legal";

    /// <summary>Titular del servicio. LSSI art. 10 y RGPD art. 13.</summary>
    public string LegalName { get; set; } = string.Empty;

    /// <summary>NIF/CIF. LSSI art. 10.</summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>Domicilio a efectos de notificaciones. LSSI art. 10.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Dirección donde se ejercen los derechos de los arts. 15-22.</summary>
    public string PrivacyEmail { get; set; } = string.Empty;

    /// <summary>Delegado de Protección de Datos, o «No designado» (art. 37).</summary>
    public string Dpo { get; set; } = string.Empty;

    /// <summary>Encargado del envío de correo (art. 28).</summary>
    public string EmailProvider { get; set; } = string.Empty;

    /// <summary>Encargado del alojamiento (art. 28).</summary>
    public string HostingProvider { get; set; } = string.Empty;

    /// <summary>Dónde se almacenan los datos. Determina si hay transferencia internacional (cap. V).</summary>
    public string HostingRegion { get; set; } = string.Empty;

    /// <summary>
    /// Enlace público a la Política de Privacidad que se cita en el pie. No sale del fichero
    /// compartido porque no es identidad, sino una ruta del cliente que puede cambiar de dominio
    /// según el despliegue; por eso vive en <c>appsettings</c> junto al resto de URLs públicas.
    /// </summary>
    public string PrivacyPolicyUrl { get; set; } = "https://app.terrenario.com/legal/privacidad";

    /// <summary>
    /// Rellena con el dato versionado todo campo que la configuración haya dejado en blanco. Se
    /// aplica como <c>PostConfigure</c>, después del binding, para que sobreescribir un campo suelto
    /// no obligue a repetir los otros siete.
    /// </summary>
    public void FillBlanksFrom(LegalEntityOptions versioned)
    {
        LegalName = Or(LegalName, versioned.LegalName);
        TaxId = Or(TaxId, versioned.TaxId);
        Address = Or(Address, versioned.Address);
        PrivacyEmail = Or(PrivacyEmail, versioned.PrivacyEmail);
        Dpo = Or(Dpo, versioned.Dpo);
        EmailProvider = Or(EmailProvider, versioned.EmailProvider);
        HostingProvider = Or(HostingProvider, versioned.HostingProvider);
        HostingRegion = Or(HostingRegion, versioned.HostingRegion);
        PrivacyPolicyUrl = Or(PrivacyPolicyUrl, versioned.PrivacyPolicyUrl);
    }

    /// <summary>
    /// Campos sin los que el pie legal quedaría cojo. Es la red de seguridad equivalente a
    /// <c>missingLegalFields</c> del cliente: debe estar siempre vacía. Cubre un hueco que el tipado
    /// de TypeScript no ve —una cadena vacía en el JSON compila igual de bien que un NIF—.
    /// </summary>
    public IReadOnlyList<string> MissingFieldsForEmailFooter()
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(LegalName)) missing.Add(nameof(LegalName));
        if (string.IsNullOrWhiteSpace(TaxId)) missing.Add(nameof(TaxId));
        if (string.IsNullOrWhiteSpace(Address)) missing.Add(nameof(Address));
        if (string.IsNullOrWhiteSpace(PrivacyEmail)) missing.Add(nameof(PrivacyEmail));
        if (string.IsNullOrWhiteSpace(PrivacyPolicyUrl)) missing.Add(nameof(PrivacyPolicyUrl));

        return missing;
    }

    private static string Or(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

/// <summary>
/// Lee la identidad versionada del recurso incrustado. Estático y cacheado: el fichero se resuelve
/// al compilar, así que no hay nada que recargar y un fallo aquí es un fallo de build, no de
/// entorno.
/// </summary>
public static class VersionedLegalEntity
{
    /// <summary>Debe coincidir con el <c>LogicalName</c> declarado en el <c>.csproj</c>.</summary>
    public const string ResourceName = "Terrenario.Api.legal-entity.json";

    private static readonly Lazy<LegalEntityOptions> Cached = new(Read);

    public static LegalEntityOptions Value => Cached.Value;

    private static LegalEntityOptions Read()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Falta el recurso incrustado '{ResourceName}'. Lo aporta "
                + "src/frontend/terrenario-web/src/config/legal-entity.json desde el .csproj.");

        // El JSON usa camelCase porque su consumidor principal es TypeScript; el binding de .NET no
        // distingue mayúsculas, así que no hace falta duplicar los nombres con atributos.
        var entity = JsonSerializer.Deserialize<LegalEntityOptions>(
                         stream,
                         new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                     ?? throw new InvalidOperationException($"El recurso '{ResourceName}' está vacío.");

        var missing = entity.MissingFieldsForEmailFooter();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"La identidad legal versionada tiene campos vacíos ({string.Join(", ", missing)}). "
                + "Ningún correo del producto puede salir sin identificar al responsable.");

        return entity;
    }
}
