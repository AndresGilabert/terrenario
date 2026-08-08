namespace Terrenario.Api.Application.Account;

/// <summary>
/// MVP-505 (HU-4, CA-5) — <b>Política de retención y expurgo</b> declarada en código, para que el
/// plazo no viva solo en un documento y pueda verificarse (que es lo que <c>MVP-503</c> tiene que
/// poder hacer).
///
/// El plazo sale de la tabla de retención de <c>docs/07-seguridad/privacidad-datos.md</c>: <b>24
/// meses</b> para los datos de una cuenta cancelada. <c>RN-041</c> extiende ese mismo criterio a todo
/// lo demás que el producto conserva por diseño y que hasta ahora no tenía plazo: Workspaces dados de
/// baja (RN-039), registros operativos eliminados lógicamente (RN-037), solicitudes de reactivación
/// cerradas o caducadas e invitaciones terminales.
///
/// «No se borra nada» era una decisión de producto legítima; «se guarda para siempre sin criterio» no
/// lo es. Esto es lo segundo convertido en lo primero.
///
/// <b>MVP-714</b> añade la sexta categoría —los tokens de refresco muertos— con un plazo propio y
/// mucho más corto. Vive aquí, junto al de 24 meses, y no en una clase nueva, porque las dos son la
/// misma política (<c>RN-041</c>) y separarlas invitaría a que la próxima categoría naciera con su
/// plazo escondido en otro sitio.
/// </summary>
public sealed class AccountRetentionPolicy
{
    /// <summary>Meses que se conserva lo dado de baja antes de purgarlo (RN-041).</summary>
    public const int RetentionMonths = 24;

    /// <summary>
    /// MVP-714 (P-071) — Días que sobrevive un token de refresco <b>muerto</b> (revocado o caducado)
    /// antes de purgarse.
    ///
    /// <b>Por qué no los 24 meses del resto</b>: lo que hay en <c>refresh_tokens</c> es un dato de
    /// sesión —hash del token, cuenta y fechas—, no histórico operativo que nadie más pueda
    /// reconstruir. Aplicarle el plazo largo sería conservador de más justo en la categoría que más
    /// filas genera: la rotación crea una fila por cada refresco, así que un usuario activo deja miles
    /// al año.
    ///
    /// <b>Por qué 30 y no menos</b>: es el mismo orden que la vida del propio token
    /// (<c>RefreshTokenOptions.LifetimeSeconds</c>, 30 días), de modo que la regla se lee como «un
    /// token muerto no dura más de lo que habría durado vivo». Y deja cuatro ciclos de la revisión
    /// operativa semanal de <c>observabilidad.md</c> para investigar una sesión sospechosa antes de
    /// que el rastro desaparezca, que es lo único que justifica conservarlo un solo día.
    ///
    /// No se hace configurable a propósito, por el mismo motivo que los 24 meses: es plazo de negocio,
    /// no cadencia de operación (ver <c>RetentionOptions</c>).
    /// </summary>
    public const int RefreshTokenRetentionDays = 30;

    /// <summary>Fecha a partir de la cual lo dado de baja en <paramref name="closedAt"/> puede purgarse.</summary>
    public DateTimeOffset PurgeDateFor(DateTimeOffset closedAt) => closedAt.AddMonths(RetentionMonths);

    /// <summary>
    /// Fecha de corte: todo lo dado de baja antes de este instante ya ha cumplido su plazo. Es lo que
    /// consulta la rutina de expurgo.
    /// </summary>
    public DateTimeOffset CutoffFrom(DateTimeOffset now) => now.AddMonths(-RetentionMonths);

    /// <summary>
    /// Corte de los tokens de refresco: todo el que murió antes de este instante ya cumplió sus
    /// <see cref="RefreshTokenRetentionDays"/> días.
    /// </summary>
    public DateTimeOffset RefreshTokenCutoffFrom(DateTimeOffset now)
        => now.AddDays(-RefreshTokenRetentionDays);
}
