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
/// </summary>
public sealed class AccountRetentionPolicy
{
    /// <summary>Meses que se conserva lo dado de baja antes de purgarlo (RN-041).</summary>
    public const int RetentionMonths = 24;

    /// <summary>Fecha a partir de la cual lo dado de baja en <paramref name="closedAt"/> puede purgarse.</summary>
    public DateTimeOffset PurgeDateFor(DateTimeOffset closedAt) => closedAt.AddMonths(RetentionMonths);

    /// <summary>
    /// Fecha de corte: todo lo dado de baja antes de este instante ya ha cumplido su plazo. Es lo que
    /// consulta la rutina de expurgo.
    /// </summary>
    public DateTimeOffset CutoffFrom(DateTimeOffset now) => now.AddMonths(-RetentionMonths);
}
