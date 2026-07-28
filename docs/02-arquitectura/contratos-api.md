---
bloque: 02-arquitectura
documento: contratos-api
actualizado_en: "2026-07-28"
---

# Contratos de API

> Contratos iniciales REST para MVP.
> Base funcional y reglas: `../01-producto/definicion-requisitos-usuario.md`, `../01-producto/reglas-de-negocio.md` y decisiones MVP cerradas.
>
> Rutas, campos y códigos de error se escriben en inglés según
> [ADR-0009](./decisiones/ADR-0009--idioma-de-identificadores-en-codigo.md). Los **valores** de los
> catálogos cerrados se mantienen en español por ser vocabulario de dominio.

---

## APIs públicas (expuestas a clientes)

| API | Versión | Especificación | Autenticación |
|-----|---------|---------------|--------------|
| Terrenario Core API | v1 | `/api/v1/openapi.json` | Bearer JWT (OIDC Google) |

---

## APIs internas (entre componentes MVP)

| Servicio origen | Servicio destino | Protocolo | Descripción |
|----------------|-----------------|-----------|-------------|
| API Core | Servicio de Email | HTTPS | Invitaciones a Workspace |
| API Core | Google OIDC | HTTPS/OIDC | Intercambio de identidad y validación de tokens |

---

## Convenciones de API

### REST

- Versionado en la URL: `/api/v1/...`
- Recursos en plural y kebab-case, en inglés: `/plots`, `/workspace-members`
- Campos de request y response en `snake_case` inglés: `access_token`, `season_id`
- Respuestas de error: siempre JSON con `{ "error": { "code": "", "message": "", "details": [] } }`
- Paginación: `?page=1&limit=20` con respuesta `{ "data": [], "meta": { "total": 0, "page": 1, "limit": 20 } }`
- Todas las respuestas incluyen `X-Request-Id` para trazabilidad.
- Concurrencia de escritura: `If-Match` obligatorio en `PATCH`/`DELETE` de entidades críticas.
- El servidor devuelve `409 CONFLICT_VERSION_MISMATCH` cuando la versión enviada no coincide.

### Eventos (mensajería asíncrona)

- Naming de eventos: `{dominio}.{entidad}.{accion}` -> ej: `workspace.member.invited`
- Payload: siempre incluir `id`, `timestamp`, `version`, `data`
- Ver eventos funcionales por módulo en `../03-modulos/{modulo}/eventos.md`

---

## Catálogos cerrados MVP

El nombre del catálogo es un identificador y va en inglés; sus valores son vocabulario de dominio
y se mantienen en español.

| Catálogo | Valores permitidos |
|---|---|
| `plot_ownership_type` | `propia`, `cedida` |
| `harvest_destination` | `venta_aceituna`, `aceite_para_venta`, `aceite_personal`, `desconocido` |
| `harvest_product` | catálogo global fijo gobernado por sistema |
| `season_status` | `planificada`, `activa`, `cerrada` |
| `worker_member_status` | `invitado`, `activo`, `revocado` |
| `invitation_channel` | `email`, `enlace` |
| `invitation_status` | `pendiente`, `aceptada`, `rechazada`, `anulada` |
| `reactivation_request_status` | `pendiente`, `solicitada`, `autorizada`, `denegada`, `cerrada` |

---

## Contratos por flujo MVP

### 0) Workspaces

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta workspace | `POST /api/v1/workspaces` | `name*` | `201 { workspace: { id, name }, access_token, expires_in }` |
| Mis workspaces | `GET /api/v1/workspaces` | — | `200 { data:[{ id, name, role, status, is_active, joined_at }], meta:{ total, active_workspace_id } }` |
| Workspace activo | `GET /api/v1/workspaces/active` | — | `200 { id, name }` |
| Cambiar workspace activo | `PUT /api/v1/workspaces/active` | `workspace_id*` | `200 { workspace: { id, name }, access_token, expires_in }` |
| Renombrar workspace activo (MVP-206) | `PATCH /api/v1/workspaces/active` | `name*` | `200 { id, name }` |
| Opciones de baja del activo (MVP-206) | `GET /api/v1/workspaces/active/closure` | — | `200 { workspace, is_owner, mode, active_owners, successor_name, candidates[] }` |
| Dar de baja el workspace activo (MVP-206) | `POST /api/v1/workspaces/active/closure` | — | `200 { outcome, workspace, new_owner_name, notified_members, emails_sent }` |
| Traspasar la propiedad (MVP-206) | `POST /api/v1/workspaces/active/transfer-ownership` | `new_owner_user_id*` | `200 { outcome: "transferred", workspace, new_owner_name }` |
| Propiedades unicas sin resolver (MVP-206) | `GET /api/v1/workspaces/ownership-obligations` | — | `200 { data:[{ workspace_id, name, other_active_members, can_transfer }], meta:{ total, is_clear } }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `name` obligatorio | `VALIDATION_REQUIRED_WORKSPACE_NAME` |
| `name` de longitud válida | `VALIDATION_WORKSPACE_NAME_LENGTH` |
| El usuario todavía no tiene ningún Workspace | `WORKSPACE_NOT_FOUND` (404) |
| `workspace_id` obligatorio al cambiar de activo | `VALIDATION_REQUIRED` (400) |
| Activar un Workspace sin membresía activa | `AUTH_WORKSPACE_FORBIDDEN` (403) |
| Dar de baja o traspasar sin ser propietario (MVP-206) | `AUTH_WORKSPACE_OWNER_REQUIRED` (403) |
| Traspasar a quien no es miembro activo (MVP-206) | `RESOURCE_NOT_FOUND` (404) |
| Traspasar la propiedad a uno mismo (MVP-206) | `BUSINESS_RULE_OWNERSHIP_TRANSFER_TO_SELF` (422) |
| Operar sobre un Workspace dado de baja (MVP-206) | `BUSINESS_RULE_WORKSPACE_DELETED` (422) |

Reglas de contexto:

| Regla | Comportamiento |
|---|---|
| El creador queda como miembro activo del Workspace | Membresía `workspace_owner` creada en la misma transacción |
| El Workspace activo viaja en el claim `workspace_id` del `access_token` | Nunca se acepta como parámetro del cliente |
| `POST /workspaces` reemite la sesión | Devuelve un `access_token` nuevo ya situado en el Workspace creado |
| `PUT /workspaces/active` reemite la sesión | Valida membresía activa, persiste el activo (`users.active_workspace_id`) y devuelve un `access_token` nuevo situado en el destino (MVP-104) |
| `GET /workspaces` solo lista membresías `activo` | Las `revocado` quedan fuera; `is_active` marca el que ejecuta las operaciones |
| Renombrar lo puede hacer cualquier miembro activo (MVP-206) | Permisos planos en MVP por RN-034. No reemite la sesión: el nombre no viaja en el token |
| Dar de baja y traspasar se restringen al propietario (MVP-206) | Afectan a la propiedad (RN-038), a diferencia del resto de operaciones planas |
| `mode` de `GET /active/closure` (MVP-206) | `auto_transfer` (hay copropietarios: la baja reasigna y el solicitante sale), `choose` (propietario único: hay que decidir), `only_delete` (propietario único sin nadie más), `not_owner` |
| La baja es **lógica** (RN-039) | `deleted_at`; el Workspace deja de resolver contexto y de aparecer en el selector, y si era el activo la sesión cae al Workspace por defecto. Ningún dato se borra |
| `outcome` de la baja (MVP-206) | `transferred` (el Workspace sigue vivo con otra persona propietaria) o `deleted` (baja lógica con aviso al resto de miembros) |
| `is_clear: false` en obligaciones de propiedad (MVP-206) | La baja de cuenta no puede completarse: hay Workspaces de propiedad única sin resolver (RN-038, CA-9). El flujo completo de baja de cuenta es alcance posterior (`MVP-999`, P-024) |

### 0.b bis) Reactivación de un Workspace dado de baja (MVP-206)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Ver enlace de reactivación | `GET /api/v1/workspaces/reactivations/{token}` | — | `200 { id, workspace, closed_by, status, expires_at, is_expired, can_request }` |
| Solicitar traspaso y reactivación | `POST /api/v1/workspaces/reactivations/{token}/request` | — | `200 { ...misma forma, status: "solicitada" }` |
| Solicitudes pendientes de mi decisión | `GET /api/v1/workspaces/reactivations` | — | `200 { data:[{ id, workspace, requested_by, requested_at, expires_at }], meta:{ total } }` |
| Autorizar traspaso y reactivación | `POST /api/v1/workspaces/reactivations/{id}/authorize` | — | `200 { workspace, new_owner_user_id }` |
| Denegar | `POST /api/v1/workspaces/reactivations/{id}/deny` | — | `204` |
| Workspaces que di de baja | `GET /api/v1/workspaces/reactivations/closed` | — | `200 { data:[{ id, name, closed_at }], meta:{ total } }` |
| Volver a levantar uno propio | `POST /api/v1/workspaces/reactivations/closed/{id}/reopen` | — | `200 { id, name }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| Enlace inexistente o dirigido a otra persona | `REACTIVATION_REQUEST_NOT_FOUND` (404) |
| Solicitud que no es de un Workspace que diera de baja quien la resuelve | `REACTIVATION_REQUEST_NOT_FOUND` (404) |
| Enlace ya utilizado | `BUSINESS_RULE_REACTIVATION_ALREADY_USED` (422) |
| Enlace caducado | `BUSINESS_RULE_REACTIVATION_EXPIRED` (422) |
| Resolver una solicitud que no se ha pedido o ya se resolvió | `BUSINESS_RULE_REACTIVATION_NOT_REQUESTED` (422) |
| Solicitar sobre un Workspace que ya está activo | `BUSINESS_RULE_WORKSPACE_NOT_DELETED` (422) |

Reglas de contexto:

| Regla | Comportamiento |
|---|---|
| Estas rutas **no** exigen Workspace activo | El Workspace está dado de baja: no resuelve contexto y puede ser el único que tuvieran las personas implicadas |
| Un enlace por persona notificada | Se emite una solicitud por miembro activo al darse de baja; el traspaso queda atado a quien lo pide, no al enlace |
| El enlace es de un solo uso y caduca | Vigencia por configuración (`WorkspaceLifecycle:ReactivationLifetimeDays`, 7 días en MVP). En base de datos vive solo el hash |
| Solo autoriza quien dio de baja (RN-040) | Para cualquier otra cuenta la solicitud no existe (404 uniforme) |
| Al autorizar | El Workspace se reactiva y su propiedad pasa al solicitante en la misma transacción; el resto de enlaces vivos pasan a `cerrada` |
| Reapertura directa | Quien dio de baja puede levantarlo sin solicitud previa; es la única vía cuando no había más miembros a los que avisar |

### 0.b) Invitaciones a Workspace

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Emitir invitación | `POST /api/v1/workspaces/invitations` | `channel*`, `email?` | `201 { id, channel, email, status, accept_url, expires_at, email_sent }` |
| Invitaciones pendientes (emitidas) | `GET /api/v1/workspaces/invitations` | — | `200 { data, meta: { total } }` |
| Reenviar invitación por email (MVP-204) | `POST /api/v1/workspaces/invitations/{id}/resend` | `deliver_email?` (def. `true`) | `200 { id, email, accept_url, expires_at, email_sent }` |
| Anular invitación pendiente (MVP-207) | `POST /api/v1/workspaces/invitations/{id}/cancel` | — | `204` |
| Ver invitación por enlace | `GET /api/v1/invitations/{token}` | — | `200 { id, channel, status, workspace, invited_by, expires_at, is_expired, viewer: { can_accept, reason } }` |
| Aceptar invitación por enlace | `POST /api/v1/invitations/{token}/accept` | — | `200 { workspace, access_token, expires_in, already_member }` |
| Rechazar invitación por enlace (MVP-107) | `POST /api/v1/invitations/{token}/reject` | — | `204` |
| Invitaciones recibidas (MVP-107) | `GET /api/v1/invitations/received` | — | `200 { data, meta: { total } }` |
| Aceptar recibida desde bandeja (MVP-107) | `POST /api/v1/invitations/received/{id}/accept` | — | `200 { workspace, access_token, expires_in, already_member }` |
| Rechazar recibida desde bandeja (MVP-107) | `POST /api/v1/invitations/received/{id}/reject` | — | `204` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `channel` dentro de `invitation_channel` | `VALIDATION_INVITATION_CHANNEL_INVALID` |
| `email` obligatorio si `channel` es `email` | `VALIDATION_REQUIRED_INVITATION_EMAIL` |
| `email` con formato válido (máx. 320 caracteres) | `VALIDATION_INVITATION_EMAIL_INVALID` |
| Sesión sin Workspace activo al invitar | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |
| La invitación por email la abre otra cuenta | `AUTH_INVITATION_EMAIL_MISMATCH` (403) |
| Token desconocido | `INVITATION_NOT_FOUND` (404) |
| Invitación caducada | `BUSINESS_RULE_INVITATION_EXPIRED` (422) |
| Invitación ya utilizada | `BUSINESS_RULE_INVITATION_ALREADY_ACCEPTED` (422) |
| Aceptar una invitación ya rechazada (MVP-107) | `BUSINESS_RULE_INVITATION_ALREADY_REJECTED` (422) |
| Aceptar o rechazar una invitación anulada por el emisor (MVP-207) | `BUSINESS_RULE_INVITATION_CANCELLED` (422) |
| El email invitado ya es miembro activo | `BUSINESS_RULE_INVITATION_ALREADY_MEMBER` (422) |
| Aceptar/rechazar por id una invitación no dirigida a la cuenta o de canal enlace (MVP-107) | `INVITATION_NOT_FOUND` (404) |

Reglas de contexto:

| Regla | Comportamiento |
|---|---|
| Cualquier miembro puede invitar | Permisos planos en MVP por RN-034 |
| El Workspace de origen no viaja en la petición | Se resuelve en servidor desde el claim `workspace_id` |
| `accept_url` solo se devuelve al emitir | En base de datos vive únicamente el hash del token: el enlace no se puede recuperar después |
| La invitación es de un solo uso y caduca | Vigencia por configuración (`Invitations:LifetimeDays`, 7 días en MVP) |
| La invitación por email va dirigida a una cuenta | Solo la acepta ese email; el canal `enlace` acepta a cualquier usuario autenticado |
| Aceptar reemite la sesión | Devuelve un `access_token` nuevo ya situado en el Workspace de la invitación |
| `email_sent: false` | La invitación es válida pero el proveedor de email falló; el enlace se comparte por otro medio |
| Reenvío (MVP-204, HU-5/CA-6) | Solo invitaciones por email **pendientes** del Workspace activo. Rota el token (un solo uso) y renueva la caducidad, igual que la emisión original; la persona sigue `invitado`. `deliver_email:false` es el reenvío "por enlace" (no reenvía el correo, solo devuelve el nuevo `accept_url`). Cualquier invitación inexistente, de otro Workspace, de canal `enlace` o no pendiente responde `INVITATION_NOT_FOUND` (404) |
| Anulación (MVP-207, HU-2/CA-4) | Solo invitaciones **pendientes** del Workspace activo, de **cualquier canal** (a diferencia del reenvío: un enlace compartible que se ha ido de las manos es justo el caso en que hace falta retirarlo). Transita a `anulada`: el enlace deja de permitir la aceptación y la persona desaparece de la lista de personas del Workspace. Idempotente en el dominio, pero una segunda llamada responde `INVITATION_NOT_FOUND` (404) porque ya no está pendiente. Cualquier invitación inexistente, de otro Workspace o no pendiente responde igualmente 404 |
| Anular frente a rechazar frente a revocar | `anulada` la fija el **Workspace emisor** sobre quien aún no ha entrado; `rechazada` (MVP-107) la fija la **persona invitada**; revocar (MVP-204, CA-7) retira a quien **ya es miembro**. Una invitación ya aceptada no se anula: se revoca el acceso |
| `viewer.can_accept` / `viewer.reason` (MVP-107) | Aptitud de la cuenta autenticada calculada antes de aceptar; `reason` ∈ `email_mismatch`, `expired`, `already_used`, `already_rejected`, `cancelled`, `already_member`. No revela el email destinatario |
| Bandeja de recibidas (MVP-107) | Solo canal `email` dirigido a la cuenta, pendiente y no caducada; se autoriza por titularidad del email, no por token. No exige Workspace activo |
| Rechazar (MVP-107) | Transita a `rechazada` sin crear membresía; no cierra sesión. Idempotente ante doble rechazo del destinatario |

### 0.c) Telemetría del embudo de login (MVP-105)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Ingesta de evento de embudo | `POST /api/v1/auth/telemetry/login` | `event*`, `flow_id*` | `202` (sin cuerpo) |

`POST /api/v1/auth/google/callback` acepta además un `flow_id` **opcional** para correlacionar el
éxito/error del intercambio con los eventos de cliente. Si no llega, el servidor genera uno.

Validaciones y reglas:

| Regla | Código error / comportamiento |
|---|---|
| `event` dentro de `{ login_screen_viewed, login_google_clicked, login_abandonment }` | `VALIDATION_REQUIRED` (400) si no |
| `flow_id` alfanumérico y de longitud válida (≤ 64) | `VALIDATION_REQUIRED` (400) si no |
| `login_google_success` / `login_google_error` no se aceptan del cliente | Son autoritativos del servidor (se emiten en el callback) |
| La traza no contiene PII | Solo `event`, `flow_id` y `channel`; nunca email ni token (RN-020, RN-017) |

> El detalle de eventos y campos mínimos del embudo vive en
> `../07-seguridad/autenticacion-autorizacion.md`. La explotación completa (dimensiones, persistencia
> y alertado) es alcance de `MVP-601`.

### Ámbito de Workspace en operaciones protegidas (MVP-105)

Toda operación de negocio Workspace-first se marca con `[RequireWorkspaceScope]`: el Workspace activo
se resuelve en servidor desde el claim `workspace_id` (RN-034) y **nunca** viaja como parámetro.

| Regla | Código error |
|---|---|
| La sesión no tiene ningún Workspace activo | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |
| El recurso no pertenece al Workspace activo (`IWorkspaceContext.EnsureInScope`) | `AUTH_WORKSPACE_FORBIDDEN` (403) |

### 1) Plots (terrenos)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta terreno | `POST /api/v1/plots` | `name*`, `ownership_type*`, `alias?`, `owner_name?`, `tree_count?`, `cadastral_reference?`, `location?` | `201 { id, workspace_id, ... }` |
| Editar terreno | `PATCH /api/v1/plots/{plotId}` | campos parciales permitidos | `200 { ...plot }` |
| Listado terrenos | `GET /api/v1/plots` | filtros: `search?`, `is_active?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `name` obligatorio y longitud válida | `VALIDATION_REQUIRED_NAME` (400) |
| `ownership_type` obligatorio (RN-028) | `VALIDATION_REQUIRED` / `VALIDATION_REQUIRED_PLOT_OWNERSHIP_TYPE` (400) |
| `ownership_type` dentro de `plot_ownership_type` | `VALIDATION_PLOT_OWNERSHIP_TYPE_INVALID` (400) |
| `tree_count >= 0` (entero) | `VALIDATION_RANGE_TREE_COUNT` (400) |
| Nombre ya usado en el Workspace, ignorando mayúsculas (MVP-207) | `CONFLICT_PLOT_NAME_DUPLICATE` (409) |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |
| Terreno inexistente o de otro Workspace | `RESOURCE_NOT_FOUND` (404) |

Reglas de contexto (MVP-202):

| Regla | Comportamiento |
|---|---|
| Alta mínima (RN-028) | Solo `name` y `ownership_type` son obligatorios; el resto es opcional e informativo |
| `tree_count` ausente | No bloquea; se marca como dato incompleto para el dashboard (RN-010). La respuesta incluye `has_tree_count` |
| Duplicados (MVP-207) | Un Workspace no admite dos terrenos con el mismo `name` ignorando mayúsculas y espacios sobrantes (índice único `(workspace_id, lower(name))`). Los **inactivos también ocupan su nombre**. El `alias` es un apodo libre y **sí** puede repetirse |
| Inactivación con histórico (CA-3) | `PATCH { is_active:false }`; reversible. No hay borrado físico de terrenos |
| `PATCH` de campos parciales | Un campo ausente mantiene su valor; presente (incluido vacío) lo asigna/limpia |
| `location` | Texto libre. Coordenadas/mapas y `soil_metadata` quedan fuera de alcance del MVP |

### 2) Seasons (temporadas)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta temporada | `POST /api/v1/seasons` | `name*`, `start_date*`, `end_date?` | `201 { ...season }` (nace **activa**) |
| Editar temporada | `PATCH /api/v1/seasons/{seasonId}` | `name?`, `start_date?`, `end_date?`, `is_closed?` | `200 { ...season }` |
| Listado temporadas | `GET /api/v1/seasons` | — (sin filtros) | `200 { data, meta: { total } }` |
| Temporada activa | `GET /api/v1/seasons/active` | — | `200 { ...season }` · `404` si no hay |
| Cambiar la temporada activa | `POST /api/v1/seasons/{seasonId}/activate` | — | `200 { ...season }` |

Todas exigen `[RequireWorkspaceScope]`. La representación de una temporada es
`{ id, workspace_id, name, start_date, end_date, is_active, is_closed, status }`, donde `status` es
el valor **derivado** del catálogo `season_status` (`planificada`/`activa`/`cerrada`), no una columna:
`cerrada` ≡ `is_closed`; `activa` ≡ `is_active` y no cerrada; `planificada` ≡ ninguna de las dos.

Validaciones clave:

| Regla | Código error |
|---|---|
| `name` ausente o en blanco en el alta | `VALIDATION_REQUIRED` (400) |
| `name` en blanco al editar | `VALIDATION_REQUIRED_SEASON_NAME` (400) |
| `name` de longitud válida (≤ 120) | `VALIDATION_SEASON_NAME_LENGTH` (400) |
| `start_date <= end_date`; fecha con formato válido (`YYYY-MM-DD`) | `VALIDATION_SEASON_DATE_RANGE` (400) |
| Nombre ya usado en el Workspace, ignorando mayúsculas (MVP-207) | `CONFLICT_SEASON_NAME_DUPLICATE` (409) |
| Temporada inexistente o de otro Workspace | `SEASON_NOT_FOUND` (404) |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |

Reglas de contexto (MVP-201 · MVP-203 · MVP-207):

| Regla | Comportamiento |
|---|---|
| La temporada creada nace **activa** | Decisión de producto «crear cambia la activa» (P-017): la nueva desbanca a la anterior, que pasa a `planificada`. No hay 409 por «ya hay una activa» |
| Una sola activa por Workspace (RN-022) | Invariante de datos: índice único parcial `ux_seasons_workspace_active`. El desbanque es atómico, en dos fases dentro de una transacción |
| `end_date` es opcional | Fecha de fin **estimada**; no se bloquea por rango operativo (RN-023 es un aviso de las historias operativas, no del maestro) |
| Cierre/reapertura (RN-024) | `PATCH { is_closed:true }` es informativo y no bloquea altas. Cerrar la activa **libera** el hueco de activa del Workspace; reabrir devuelve a `planificada`, sin activar |
| Duplicados (MVP-207) | Un Workspace no admite dos temporadas con el mismo nombre ignorando mayúsculas y espacios sobrantes (índice único `(workspace_id, lower(name))`). Las **cerradas también ocupan su nombre**: cerrar no lo libera |
| `PATCH` de campos parciales | Un campo ausente mantiene su valor. El cambio de activa **no** va aquí: es `POST /seasons/{id}/activate` |
| Orden del listado | Activa primero, luego las abiertas y por último las cerradas, por fecha de inicio descendente |
| No hay borrado | Las temporadas con histórico se cierran, no se eliminan |

### 3) Tasks (tareas)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta tarea | `POST /api/v1/tasks` | `name*`, `is_active?` | `201 { id, workspace_id, name, is_active }` |
| Editar tarea | `PATCH /api/v1/tasks/{taskId}` | `name?`, `is_active?` | `200 { ...task }` |
| Listado tareas | `GET /api/v1/tasks` | `is_active?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `name` ausente o en blanco en el alta | `VALIDATION_REQUIRED` (400) |
| `name` en blanco al editar | `VALIDATION_REQUIRED_TASK_NAME` (400) |
| `name` de longitud válida (≤ 120) | `VALIDATION_TASK_NAME_LENGTH` (400) |
| Nombre ya usado en el Workspace, ignorando mayúsculas | `CONFLICT_TASK_NAME_DUPLICATE` (409) |
| Tarea inexistente o de otro Workspace | `RESOURCE_NOT_FOUND` (404) |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |

Reglas de contexto (MVP-205):

| Regla | Comportamiento |
|---|---|
| Catálogo por Workspace (RN-026) | Arranca **vacío** y es editable por cualquier miembro activo (RN-034). El aislamiento por Workspace lo garantiza `[RequireWorkspaceScope]`: el catálogo de un Workspace no afecta al de otro |
| Duplicados | Un Workspace no admite dos tareas con el mismo nombre ignorando mayúsculas y espacios sobrantes (índice único `(workspace_id, lower(name))`). Las **inactivas también ocupan su nombre**: se reactivan, no se duplican. No hay normalización de acentos: «Poda» y «Podá» conviven |
| Inactivación con histórico (CA-3) | `PATCH { is_active:false }`; reversible. No hay borrado físico de tareas |
| `PATCH` de campos parciales | Un campo ausente mantiene su valor |
| Orden del listado | Activas primero y luego por nombre. La operativa diaria pedirá `is_active=true` |
| Tarea en la actividad (RN-025) | La tarea es obligatoria y puede venir del catálogo (`task_id`) o de texto libre (`task_text`); guardar una tarea libre en el catálogo es alcance de MVP-302 y reutiliza esta guarda de duplicados |

### 4) Workers (trabajadores)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta trabajador | `POST /api/v1/workers` | `name*`, `hourly_rate?` | `201 { id, workspace_id, name, hourly_rate, is_active }` |
| Editar trabajador | `PATCH /api/v1/workers/{workerId}` | `name?`, `hourly_rate?`, `is_active?` | `200 { ...worker }` |
| Listado trabajadores | `GET /api/v1/workers` | `is_active?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `name` obligatorio | `VALIDATION_REQUIRED_NAME` (400) |
| `name` de longitud válida (≤ 150) | `VALIDATION_WORKER_NAME_LENGTH` (400) |
| `hourly_rate >= 0` (opcional, de referencia) | `VALIDATION_RANGE_HOURLY_RATE` (400) |
| Nombre ya usado en el Workspace, ignorando mayúsculas (MVP-207) | `CONFLICT_WORKER_NAME_DUPLICATE` (409) |
| trabajador inexistente o de otro Workspace | `RESOURCE_NOT_FOUND` (404) |

Reglas de contexto (MVP-204):

| Regla | Comportamiento |
|---|---|
| Alcance del maestro | Solo trabajadores **sin cuenta vinculada** (cuadrilla). Los miembros del Workspace se exponen como seleccionables aparte (RN-027), desde `GET /workspace-members` |
| `hourly_rate` | Opcional y de referencia; no automatiza el coste (RN-003). `PATCH { hourly_rate: null }` la limpia |
| Duplicados (MVP-207) | Un Workspace no admite dos trabajadores con el mismo nombre ignorando mayúsculas y espacios sobrantes (índice único `(workspace_id, lower(name))`). Los **inactivos también ocupan su nombre**. No hay normalización de acentos: «Perez» y «Pérez» conviven |
| Inactivación con histórico (CA-3) | `PATCH { is_active:false }`; reversible. No hay borrado físico de trabajadores |
| `PATCH` de campos parciales | Un campo ausente mantiene su valor; presente (incluido vacío) lo asigna/limpia |

### 4.b) Workspace members (personas del Workspace, MVP-204)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Personas del Workspace | `GET /api/v1/workspace-members` | — | `200 { data, meta:{ total, active, invited, revoked } }` |
| Revocar acceso de un miembro | `POST /api/v1/workspace-members/{userId}/revoke` | — | `204` |

`GET /workspace-members` devuelve una **lista unificada** con el estado de membresía
(`worker_member_status`): las membresías reales (`activo`/`revocado`, `kind: "member"`) más las
invitaciones por email pendientes proyectadas como `invitado` (`kind: "invitation"`). El estado
`invitado` **no** es una fila de `workspace_members`: se combina desde `workspace_invitations` (el
canal `enlace` no tiene destinatario, así que no genera persona). Orden: activos, invitados,
revocados. Cada persona incluye señales de UI: `is_self` y `can_revoke` (miembros), `is_expired`
(invitaciones).

Validaciones y reglas:

| Regla | Código error / comportamiento |
|---|---|
| Cualquier miembro activo puede listar y revocar | Permisos planos en MVP (RN-034) |
| La persona no es un miembro activo del Workspace | `RESOURCE_NOT_FOUND` (404) |
| No se puede revocar al propietario único (CA-8) | `BUSINESS_RULE_CANNOT_REVOKE_OWNER` (422) |
| No se puede revocar al último miembro activo (CA-8) | `BUSINESS_RULE_LAST_ACTIVE_MEMBER` (422) |
| Revocar (CA-7) | La membresía pasa a `revocado`: deja de resolver contexto y de aparecer en el selector (MVP-104), sin borrar el vínculo ni los registros que ese usuario creó |
| Reingreso de un revocado | Por una invitación nueva (MVP-103); no hay reactivación directa |

### 5) Activities (actividades)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta actividad | `POST /api/v1/activities` | `date*`, `plot_id*`, `season_id*`, `worker_id*`, `task_id?`, `task_text?`, `hours*`, `manual_cost*`, `description?` | `201 { id, ...activity }` |
| Editar actividad | `PATCH /api/v1/activities/{activityId}` | campos parciales | `200 { ...activity }` |
| Listado actividades | `GET /api/v1/activities` | `from?`, `to?`, `plot_id?`, `season_id?`, `worker_id?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| responsable y horas obligatorios | `VALIDATION_ACTIVITY_REQUIRED_FIELDS` |
| tarea obligatoria por catálogo o texto libre | `VALIDATION_ACTIVITY_TASK_REQUIRED` |
| `hours > 0` | `VALIDATION_ACTIVITY_HOURS_RANGE` |
| `manual_cost >= 0` | `VALIDATION_ACTIVITY_COST_RANGE` |
| Integridad de workspace en FKs | `FOREIGN_KEY_WORKSPACE_MISMATCH` |

### 6) Harvests (cosechas)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta cosecha | `POST /api/v1/harvests` | `date*`, `plot_id*`, `season_id*`, `product*`, `kgs*`, `destination*`, `yield?`, `liters?` | `201 { id, ...harvest }` |
| Editar cosecha | `PATCH /api/v1/harvests/{harvestId}` | campos parciales | `200 { ...harvest }` |
| Listado cosechas | `GET /api/v1/harvests` | `from?`, `to?`, `plot_id?`, `season_id?`, `destination?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `product` obligatorio y dentro de catálogo cerrado | `VALIDATION_PRODUCT_INVALID` |
| `kgs` obligatorio y > 0 | `VALIDATION_HARVEST_KGS_REQUIRED` |
| `yield` y `liters` no pueden coexistir | `VALIDATION_HARVEST_XOR_YIELD_LITERS` |
| destino en catálogo cerrado | `VALIDATION_DESTINATION_INVALID` |

### 7) Purchases (compras)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta compra | `POST /api/v1/purchases` | `purchase_date*`, `product*`, `total_quantity*`, `total_cost*`, `season_id*` | `201 { id, unit_price, ... }` |
| Editar compra | `PATCH /api/v1/purchases/{purchaseId}` | campos parciales | `200 { ...purchase }` |
| Listado compras | `GET /api/v1/purchases` | `product?`, `season_id?` | `200 { data, meta }` |
| Imputar compra a terreno | `POST /api/v1/purchases/{purchaseId}/consumptions` | `plot_id*`, `quantity*` | `201 { id, purchase_id, plot_id, quantity, proportional_cost }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `total_quantity > 0` y `total_cost > 0` | `VALIDATION_PURCHASE_TOTALS_RANGE` |
| suma imputaciones <= cantidad total | `VALIDATION_CONSUMPTION_OVERFLOW` |

### 8) Dashboard

| Operación | Método y ruta | Request (query) | Respuesta 2xx |
|---|---|---|---|
| Resumen temporada | `GET /api/v1/dashboard/summary` | `season_id?`, `plot_ids?[]` | `200 { total_kg, total_liters, average_yield, kg_per_tree, incomplete }` |
| Kg por destino | `GET /api/v1/dashboard/kg-by-destination` | `season_id?`, `plot_ids?[]` | `200 { data:[{ destination, kg }] }` |
| Kg por terreno | `GET /api/v1/dashboard/kg-by-plot` | `season_id?` | `200 { data:[{ plot_id, plot_name, kg }] }` |
| Evolución rendimiento | `GET /api/v1/dashboard/yield-evolution` | `season_id?`, `granularity?=month\|week` | `200 { data:[{ period, yield }] }` |

Reglas de filtro por defecto:

| Regla | Comportamiento |
|---|---|
| Sin `season_id` | backend resuelve temporada activa del workspace |
| Sin `plot_ids` | backend usa todos los terrenos activos del workspace |

### 9) Alcance de sincronización MVP

MVP v1 opera en modo online. No se incluyen endpoints de sincronización diferida u outbox en `v1`.

Los endpoints de sincronización se definirán en una versión posterior cuando se active el alcance offline.

---

## Esquemas JSON mínimos (extracto)

### `HarvestCreateRequest`

```json
{
  "date": "2026-10-05",
  "plot_id": "uuid",
  "season_id": "uuid",
  "product": "aceituna_olivar",
  "kgs": 1200.5,
  "destination": "aceite_para_venta",
  "yield": 18.5,
  "liters": null
}
```

Regla: `yield` y `liters` son opcionales, pero no se permite informar ambos a la vez.

### `ActivityCreateRequest`

```json
{
  "date": "2026-09-20",
  "plot_id": "uuid",
  "season_id": "uuid",
  "worker_id": "uuid",
  "task_text": "Poda de mantenimiento",
  "hours": 4.5,
  "manual_cost": 70.0,
  "description": "Poda de mantenimiento"
}
```

---

## Errores estándar

| HTTP | Código | Uso |
|---|---|---|
| 400 | `VALIDATION_*` | Error de campos o formato |
| 401 | `AUTH_UNAUTHENTICATED` | Token ausente/inválido |
| 403 | `AUTH_WORKSPACE_FORBIDDEN` | Acceso fuera de workspace |
| 403 | `AUTH_WORKSPACE_SCOPE_REQUIRED` | Operación que exige Workspace activo en la sesión |
| 403 | `AUTH_WORKSPACE_OWNER_REQUIRED` | Operación reservada al propietario del Workspace (baja y traspaso, RN-038) |
| 404 | `RESOURCE_NOT_FOUND` | Recurso inexistente |
| 409 | `CONFLICT_VERSION_MISMATCH` | Colisión de versión por edición concurrente |
| 422 | `BUSINESS_RULE_*` | Regla de negocio incumplida |
| 500 | `INTERNAL_ERROR` | Error inesperado trazable por `X-Request-Id` |

---

## Política de versionado y breaking changes

Antes de introducir un breaking change, crear ADR y publicar changelog técnico.

1. APIs públicas: deprecación mínima 3 meses.
2. APIs internas: coordinación mínima 1 sprint.
3. Campos nuevos: siempre aditivos cuando sea posible.
4. Eliminación de campos: solo en cambio mayor (`/v2`).
