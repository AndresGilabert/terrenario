---
id: "MVP-206"
tipo: feature
titulo: "Ciclo de vida del Workspace: renombrar, baja lógica y traspaso de propiedad"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "8d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-102", "MVP-104", "MVP-204"]
bloquea: []
relacionado_con: ["MVP-103", "MVP-107"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["workspaces", "membresia", "propiedad", "ciclo-de-vida"]
  modulo_path: "03-modulos/"
  componentes: ["workspaces", "workspace-owner", "notificaciones"]
  etiquetas: ["mvp", "workspace", "ownership", "soft-delete"]
  nivel_riesgo: alto
creado_en: "2026-07-28"
actualizado_en: "2026-07-28"
---

# MVP-206 — Ciclo de vida del Workspace: renombrar, baja lógica y traspaso de propiedad

## Contexto

El Workspace ya se puede **crear** (el primero en MVP-102 y adicionales en MVP-107), **cambiar de
activo** (MVP-104) y **administrar sus miembros** (MVP-204: listar, revocar, reenviar invitaciones).
Falta cerrar el ciclo de vida: **renombrar** y **dar de baja** un Workspace, y gobernar su
**propiedad** cuando el propietario deja de estar.

Esta historia formaliza el punto **P-004** de `MVP-999` (detectado en MVP-102/104 y replanteado
durante MVP-204): tras poder crear Workspaces adicionales, el usuario no puede renombrarlos ni
eliminarlos, y no hay reglas de propiedad cuando el único propietario se va. MVP-204 dejó
explícitamente fuera la **transferencia de propiedad**; esta historia la introduce.

La regla rectora es que **un Workspace nunca queda sin propietario ni se pierde por accidente**: la
baja es **lógica** (nunca borrado físico) y la salida del propietario siempre resuelve la propiedad.

## Objetivo

Permitir editar el nombre de un Workspace y darlo de baja de forma segura y reversible, sin dejar
Workspaces huérfanos ni pérdida de datos, con un traspaso de propiedad explícito (propietario único)
o automático (varios propietarios), y con una vía de **reactivación** solicitable por los miembros y
autorizada por quien lo dio de baja.

## Requisitos de usuario

### HU-1 — Renombrar un Workspace

**Como** miembro del Workspace,
**quiero** cambiar el nombre del Workspace,
**para** corregir errores o reflejar cómo llamo a mi explotación.

### HU-2 — Dar de baja un Workspace sin perder los datos

**Como** propietario del Workspace,
**quiero** dar de baja un Workspace que ya no uso,
**para** dejar de verlo, sabiendo que no se borra físicamente y podría recuperarse.

### HU-3 — No quedarme sin propietario al irme (propietario único)

**Como** propietario único que da de baja el Workspace o borra su cuenta,
**quiero** que el sistema me pida decidir entre **traspasar** la propiedad a otra persona o
**dar de baja** el Workspace,
**para** que el Workspace no quede huérfano ni se pierda por mi salida.

### HU-4 — Traspaso automático cuando hay varios propietarios

**Como** copropietario que da de baja el Workspace,
**quiero** que, si existen otros propietarios, el Workspace pase directamente a uno de ellos y siga
vivo,
**para** no interrumpir el trabajo de los demás por mi salida.

### HU-5 — Enterarme si se da de baja un Workspace del que soy miembro

**Como** miembro (no propietario) de un Workspace que ha sido dado de baja,
**quiero** recibir un email informándome y con un enlace para **solicitar su traspaso y
reactivación**,
**para** poder recuperarlo si todavía lo necesito.

### HU-6 — Autorizar la reactivación que solicita otro miembro

**Como** persona que dio de baja el Workspace,
**quiero** autorizar (o no) la solicitud de traspaso y reactivación de un miembro,
**para** controlar quién recupera el Workspace y con qué propiedad.

## Alcance (in-scope)

- **Renombrar** un Workspace (`name`), con las validaciones de nombre ya vigentes (MVP-102).
- **Baja lógica** de un Workspace: se marca como eliminado (`deleted_at`), **nunca** borrado físico.
  Un Workspace dado de baja deja de resolver contexto activo y de aparecer en el selector (MVP-104),
  y sus recursos con ámbito de Workspace dejan de ser accesibles, sin borrarse.
- **Traspaso de propiedad** (`workspace_owner`):
  - **Propietario único** que da de baja el Workspace o borra su cuenta: el sistema **pregunta** y el
    usuario decide **traspasar** (elige a qué miembro) o **dar de baja**.
  - **Varios propietarios**: al dar de baja, el Workspace **queda asociado automáticamente** a otro
    propietario activo y sigue vivo; el solicitante deja de ser propietario.
- **Notificación por email** al resto de miembros cuando un Workspace se da de baja, con un **enlace
  de un solo uso** para **solicitar su traspaso y reactivación**.
- **Autorización de la reactivación**: la solicitud la **autoriza quien dio de baja** el Workspace;
  al autorizarse, el Workspace se reactiva (`deleted_at` a nulo) y la propiedad pasa al solicitante.
- **Regla de no-orfandad en la baja de cuenta**: la baja de la cuenta de un usuario que sea
  propietario único de uno o más Workspaces **exige resolver** cada uno (traspaso o baja lógica)
  antes de completar la baja, reutilizando la misma decisión de HU-3.

## Fuera de alcance (out-of-scope)

- El **flujo completo de baja de cuenta** (RGPD / derecho de supresión) como funcionalidad de UI y de
  borrado de datos personales: esta historia solo define e implementa la **regla de no-orfandad** que
  ese flujo deberá respetar. El resto de la baja de cuenta se planifica como historia propia (ver
  `MVP-999`, nuevo punto).
- **Borrado físico** o purga de Workspaces dados de baja (retención, expurgo programado).
- **Permisos granulares** más allá de los planos de MVP (RN-034): en MVP el rol `workspace_owner` es
  informativo salvo para las reglas de propiedad de esta historia.
- Traspaso de propiedad **fuera** del contexto de baja (p. ej. "ceder la propiedad" como acción
  independiente sin dar de baja) — se puede evaluar después si hay demanda.

## Criterios de aceptación

> Marcados en la revisión de cierre de la épica (`MVP-299`, 2026-07-28, hallazgo R-02): estaban sin
> marcar pese a estar implementados y verificados. Entre paréntesis, la evidencia de cada uno.

- [x] **CA-1**: Un miembro puede renombrar el Workspace activo; el nuevo nombre se refleja en el
  selector y en la cabecera sin recrear la sesión. _(`PATCH /workspaces/active` verificado en API y
  UI, por propietario y por miembro no propietario.)_
- [x] **CA-2**: Dar de baja un Workspace es una **baja lógica** (`deleted_at`), nunca un borrado
  físico: los datos siguen en base de datos y el Workspace deja de resolver contexto y de aparecer en
  el selector. _(`POST /workspaces/active/closure` → `outcome: "deleted"`; fila y datos intactos en
  PostgreSQL; desaparece de `GET /workspaces`.)_
- [x] **CA-3**: El sistema **impide dejar un Workspace sin propietario**. Si el propietario único da
  de baja el Workspace o borra su cuenta, se le exige decidir entre traspasar o dar de baja antes de
  completar la acción. _(`GET /workspaces/active/closure` devuelve `mode` por caso; confirmación
  deshabilitada hasta decidir; `WorkspaceOwnershipGuard` cubierto por tests.)_
- [x] **CA-4**: En el traspaso, el usuario que realiza la acción **elige** a qué persona (miembro
  activo) otorga la propiedad; esa persona pasa a `workspace_owner` y el Workspace sigue activo.
  _(`POST /workspaces/active/transfer-ownership` con `candidates[]` del endpoint de opciones;
  `TransferWorkspaceOwnershipHandlerTests`.)_
- [x] **CA-5**: Si existen **varios propietarios**, dar de baja el Workspace lo **reasigna
  automáticamente** a otro propietario activo y el Workspace sigue vivo; no se da de baja ni se pide
  elegir. _(`mode: auto_transfer` y `FindOtherActiveOwnerAsync`; `CloseWorkspaceHandlerTests`.)_
- [x] **CA-6**: Al dar de baja un Workspace, el resto de miembros recibe un **email** informando de la
  baja, con un **enlace de un solo uso** para solicitar su traspaso y reactivación. _(Verificado por
  `WorkspaceLifecycleEmailComposerTests` y el contador `notified_members`/`emails_sent` de la
  respuesta; no ejercitado end-to-end porque el entorno de desarrollo no tiene un segundo miembro.)_
- [x] **CA-7**: Un miembro puede **solicitar** la reactivación desde ese enlace; la solicitud debe ser
  **autorizada por quien dio de baja** el Workspace. Al autorizarse, el Workspace se **reactiva** y la
  propiedad pasa al solicitante. _(`/reactivations/:token` y `/reactivations`;
  `ReactivationHandlersTests`.)_
- [x] **CA-8**: Un Workspace dado de baja **no** resuelve contexto activo ni aparece en ningún
  selector; si era el activo de algún usuario, la sesión cae al Workspace por defecto (MVP-104).
  _(Verificado end-to-end: tras la baja, un token con ese `workspace_id` deja de resolverlo y los
  recursos con ámbito de Workspace responden ya desde el Workspace por defecto.)_
- [x] **CA-9**: La baja de la cuenta de un propietario único **no deja Workspaces huérfanos**: obliga
  a resolver cada uno (traspaso o baja lógica) con la misma decisión de HU-3. _(Punto de enganche
  entregado: `GET /workspaces/ownership-obligations` y
  `WorkspaceOwnershipGuard.EnsureAccountClosureAllowedAsync`. El flujo de baja de cuenta que los
  consume es P-024, fuera de alcance.)_
- [x] **CA-10**: El enlace de reactivación es de **un solo uso**, con caducidad, y solo la persona que
  dio de baja el Workspace puede autorizar el traspaso; nadie más puede reactivarlo por esa vía.
  _(`token_hash` + `authorized_by_user_id`; `WorkspaceReactivationRequestTests` y
  `WorkspaceLifecycleRepositorySqliteTests`.)_

## Árbol de decisión (baja de Workspace / de cuenta del propietario)

Cuando un usuario con rol `workspace_owner` solicita dar de baja un Workspace (o borra su cuenta, que
se aplica a cada Workspace del que sea propietario único):

1. **¿Hay otros propietarios activos?**
   - **Sí** → el Workspace **queda asociado automáticamente** a otro propietario activo (CA-5); sigue
     vivo. El solicitante deja de ser propietario (y, si borra su cuenta, deja de ser miembro).
   - **No** (propietario único) → se le **pregunta** (CA-3):
     - **Traspasar** → elige un miembro activo, que pasa a `workspace_owner` (CA-4); el Workspace
       sigue vivo.
     - **Dar de baja** → **baja lógica** (CA-2); se **notifica por email** a los demás miembros con el
       enlace de reactivación (CA-6). La reactivación la autoriza quien dio de baja (CA-7/CA-10).
2. **Propietario único sin ningún otro miembro** → solo cabe la baja lógica; no hay a quién traspasar
   ni a quién notificar. La reactivación queda disponible solo para quien la dio de baja.

## Maquetas y referencias visuales

- No hay pantalla de prototipo específica para renombrar/eliminar/traspasar. La superficie natural es
  la administración del Workspace, contigua a «Miembros y accesos» (MVP-204) y al selector
  (`WorkspaceSwitcher`, MVP-104). Referencia de estilo del prototipo:
  [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| AjustesView · datos del Workspace | RN-034 (renombrar lo puede hacer cualquier miembro) | cubierto | `/app/ajustes`: renombrado por propietario y por miembro no propietario verificado en API y UI; nombre reflejado en selector y cabecera sin recrear sesión (CA-1) |
| AjustesView · zona nueva de propiedad y baja | RN-038 / RN-039 / RN-040 | cubierto | Diálogo por caso (`auto_transfer`/`choose`/`only_delete`), confirmación deshabilitada hasta decidir (CA-3), traspaso y baja verificados en API + base de datos + UI |
| Pantallas nuevas de reactivación (no existen en el prototipo) | RN-040 | cubierto | `/reactivations/:token` (solicitar, un solo uso) y `/reactivations` (autorizar/denegar y reabrir) verificadas end-to-end |
| AjustesView · «Perfil del titular» | RN-036 (identidad de Google, no editable) | fuera de alcance | No se porta; registrado como P-032 en `MVP-999` |

## Notas y decisiones

- **Corregido en la 3ª pasada de `MVP-299` (2026-07-28): la reasignación de CA-5 y el maestro de
  responsables.** Al ceder el Workspace, la membresía de quien sale pasa a `revocado`, pero
  `CloseWorkspaceHandler.ReassignAsync` no retiraba su fila del maestro de `workers` que materializó
  `MVP-208`, así que seguía apareciendo como responsable seleccionable —y como «MIEMBRO» activo en
  Trabajadores— de un Workspace al que ya no pertenece. Es la costura entre esta historia y `MVP-208`:
  hay **dos** vías que revocan una membresía y solo la de `RevokeMemberHandler` mantenía el maestro
  alineado. Hallazgo `R-25`, corregido en `MVP-299` (CA-4) con dos tests de regresión. El
  comportamiento de esta historia no cambia: la baja lógica (`deleted`) no revoca a nadie y sigue sin
  tocar el maestro.
- **Reglas de negocio nuevas a formalizar** en `docs/01-producto/reglas-de-negocio.md` al refinar
  (propuesta): «un Workspace nunca queda sin propietario»; «la baja de Workspace es lógica, no
  física»; «la salida del propietario resuelve siempre la propiedad (traspaso o baja)». Se dejan
  enunciadas aquí para no tocar el catálogo global hasta el refinamiento.
  **Formalizadas al implementar** como **RN-038** (un Workspace nunca queda sin propietario, incluida
  la no-orfandad en la baja de cuenta), **RN-039** (la baja es lógica, nunca física) y **RN-040** (la
  reactivación la autoriza quien dio de baja).
- **Modelo de datos previsto** (a cerrar en el `tech-design`): `workspaces` gana `deleted_at`
  (timestamptz, nullable) y `deleted_by_user_id`; el traspaso actualiza `workspaces.owner_id` y los
  roles de `workspace_members`. La reactivación se modela con una entidad de **solicitud** con
  `token_hash` (un solo uso), `requested_by_user_id`, `authorized_by_user_id` (= quien dio de baja),
  estado y caducidad, reutilizando el patrón de tokens de las invitaciones (MVP-103).
- **Permisos (RN-034).** En MVP los permisos son planos; renombrar podría permitirse a cualquier
  miembro. Las acciones de **baja** y **traspaso** afectan a la propiedad, por lo que se restringen a
  `workspace_owner`. **Decisión de refinamiento pendiente con el PO**: ¿renombrar lo puede hacer
  cualquier miembro (coherente con RN-034 y con la revocación de MVP-204) o solo el propietario?
  **Decisión del PO (2026-07-28): cualquier miembro activo**, por la literalidad de HU-1 y por
  coherencia con RN-034 y con la revocación de MVP-204.
- **Usabilidad — «dar de baja» con varios propietarios.** Con copropietarios, la acción no elimina el
  Workspace sino que lo reasigna y saca al solicitante; conviene que la UI **nombre la acción con
  claridad** (p. ej. «Salir y ceder mi propiedad») para no dar a entender un borrado. A decidir en el
  `tech-design`. **Decisión del PO (2026-07-28)**: con copropietarios el solicitante **sale del
  Workspace** (cede la propiedad y su membresía pasa a `revocado`), y la UI llama a la acción «Salir
  y ceder mi propiedad» anunciando a quién pasa. En cambio, el **traspaso explícito** del propietario
  único **no expulsa**: quien traspasa se queda como miembro normal (para irse está la retirada de
  acceso de MVP-204). Las acciones operan siempre sobre el **Workspace activo**, desde «Ajustes».
- **Edge — el que dio de baja borra su cuenta.** Si quien tiene que autorizar la reactivación ya no
  existe, la solicitud no puede autorizarse por esa vía. Por eso la baja de cuenta del propietario
  único **no** usa la vía de baja-lógica-con-reactivación por defecto, sino que **fuerza el traspaso**
  (CA-9); la baja lógica con reactivación aplica a la baja de Workspace por un usuario que sigue
  existiendo. A confirmar en el refinamiento.
- **Impacto en MVP-104/204.** El selector y el contexto activo ya filtran por membresía `activo`;
  además deberán excluir Workspaces con `deleted_at`. El traspaso reutiliza el modelo de membresía de
  MVP-104 (cambio de `role`). Sin retrabajo funcional de lo entregado, solo el filtro de baja lógica.
- **Origen y trazabilidad.** Cierra **P-004** de `MVP-999` (renombrar/eliminar Workspace), ampliado
  con la casuística de propiedad (traspaso explícito/automático, notificación y reactivación) aportada
  por el PO el 2026-07-28.
