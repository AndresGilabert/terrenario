namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-603 — Ajustes de la capa de operación: a quién se avisa y con qué llave se consultan las
/// señales. Los <b>umbrales no se configuran aquí</b>: los fija la KB, y poder bajarlos desde un ajuste
/// de despliegue convertiría un SLO acordado en una preferencia.
/// </summary>
public sealed class OpsOptions
{
    public const string SectionName = "Ops";

    /// <summary>
    /// Llave de servicio para consultar <c>GET /api/v1/ops/signals</c>. Es autenticación M2M de las que
    /// contempla <c>docs/07-seguridad/autenticacion-autorizacion.md</c>, no una sesión de usuario: quien
    /// consulta esto es el equipo, no una persona con cuenta.
    ///
    /// <b>Secreto: nunca en `appsettings`, y tampoco como marcador.</b> Sin valor, el endpoint no existe
    /// (404) en lugar de quedar abierto: si alguna vez se despliega sin configurarlo, el fallo es que no
    /// se puede consultar, no que lo pueda consultar cualquiera.
    ///
    /// El resto de secretos del producto sí llevan un marcador <c>REPLACE_IN_SECRETS</c> en
    /// <c>appsettings.json</c>, y aquí <b>no puede llevarlo</b>: los demás rompen ruidosamente si nadie
    /// los sustituye —la base de datos no conecta, el login falla—, mientras que un marcador aquí
    /// **abriría** el endpoint con una llave que está publicada en un repositorio público.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Destinatario de los avisos de alerta. Sin él las alertas solo quedan en la traza.
    /// </summary>
    public string AlertEmail { get; set; } = string.Empty;

    /// <summary>
    /// Permite apagar la vigilancia. Se desactiva en los tests de API y, desde <c>MVP-699</c> (`R-05`),
    /// también en <b>desarrollo</b>: una máquina de trabajo con cuenta de envío y destinatario
    /// configurados mandaba correos de alerta reales por cualquier error transitorio mientras se
    /// programa. Las alertas se prueban con sus tests, no dejándolas sueltas en local.
    /// </summary>
    public bool AlertsEnabled { get; set; } = true;

    /// <summary>
    /// MKT-101 — Permite apagar el resumen operativo periódico. Mismo motivo que
    /// <see cref="AlertsEnabled"/>: se desactiva en los tests de API y en desarrollo para que una
    /// máquina de trabajo con cuenta de envío y destinatario configurados no mande resúmenes reales.
    /// </summary>
    public bool SummaryEnabled { get; set; } = true;

    public bool IsSignalsEndpointEnabled => !string.IsNullOrWhiteSpace(ApiKey);
}
