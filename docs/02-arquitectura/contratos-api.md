---
bloque: 02-arquitectura
documento: contratos-api
actualizado_en: "2026-08-08"
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
- **El cuerpo debe ir codificado en UTF-8.** Un cuerpo con bytes que no lo son responde `400`
  `VALIDATION_FORMAT_INVALID`, no `500`: es un error de quien llama (MVP-502, `P-027`).
- **Los mensajes de error van siempre en español.** Ningún texto por defecto del framework llega al
  cliente (MVP-502, `P-043`).

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
| `harvest_yield_unit` (MVP-402) | `l_100kg` (canónica, RN-013), `kg_100kg` (rendimiento graso, se convierte con la densidad de RN-016). Es **unidad de entrada**, no un campo del recurso: lo persistido es siempre la canónica |
| `harvest_destination` | `venta_aceituna`, `aceite_para_venta`, `aceite_personal`, `desconocido` |
| `harvest_product` | `aceituna_olivar` — catálogo global fijo gobernado por sistema (`MVP-401`). Un solo valor en el MVP: la **variedad** pertenece al terreno y el **producto** debería vivir a nivel de Workspace modulando el cálculo de rendimiento; ambas cosas son ampliación posterior (`MVP-999`, `P-059`/`P-060`) |
| `season_status` (MVP-209) | `planificada`, `abierta`, `cerrada` — **derivado** de `is_closed` + `start_date` vs hoy, no persistido. Es el **estado** de la campaña, independiente de la temporada de trabajo (`is_working`) |
| `worker_member_status` | `invitado`, `activo`, `revocado` |
| `worker_kind` | `member`, `crew` (identificador de clase de responsable; derivado de `user_account_id`) |
| `invitation_channel` | `email`, `enlace` |
| `invitation_status` | `pendiente`, `aceptada`, `rechazada`, `anulada` |
| `reactivation_request_status` | `pendiente`, `solicitada`, `autorizada`, `denegada`, `cerrada` |
| `diary_entry_type` (MVP-305) | `actividad`, `compra`, `consumo`, `cosecha` (los cuatro vivos desde `MVP-401`) |
| `feedback_kind` (MVP-711) | `incidencia`, `sugerencia` — solo dos: quien está atascado no debería tener que clasificar su problema |

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
| `transferred` revoca a quien lo pidió (MVP-299, `R-25`) | Su membresía pasa a `revocado` **y su fila de `workers` se inactiva**, igual que al retirarle el acceso a mano: deja de ser responsable seleccionable sin invalidar lo que ya la referencie (MVP-208, CA-4). La baja `deleted` no revoca a nadie, así que no toca el maestro |
| `is_clear: false` en obligaciones de propiedad (MVP-206) | La baja de cuenta no puede completarse: hay Workspaces de propiedad única sin resolver (RN-038, CA-9). El flujo completo de baja de cuenta es alcance posterior (`MVP-999`, P-024) |

### 0.a bis) Account (baja de cuenta, MVP-505)

| Operación | Método y ruta | Request | Respuesta 2xx |
|---|---|---|---|
| Qué bloquea la baja y qué alcance tendrá | `GET /api/v1/account/closure` | — | `200 { is_clear, obligations[], active_memberships, active_sessions, confirmation_phrase, retention_months }` |
| Eliminar la cuenta | `POST /api/v1/account/closure` | `confirmation*` | `200 { revoked_sessions, revoked_memberships, cancelled_invitations, purge_after }` |

Es el **derecho de supresión** (RGPD art. 17) ejercido por la propia persona, sin escribir a nadie.

**No exige ámbito de Workspace**, a diferencia del resto de recursos: la baja es de la *cuenta*, y
quien no tenga ningún Workspace —o lo haya perdido— también tiene derecho a ejercerla.

| Regla | Comportamiento |
|---|---|
| Confirmación explícita (CA-3) | `confirmation` debe ser exactamente la frase que devuelve el `GET` (`ELIMINAR MI CUENTA`), sensible a mayúsculas. Se comprueba **en servidor**: una operación irreversible no puede depender de que el cliente se porte bien. Si no coincide, `400` |
| No-orfandad (CA-4, RN-038) | Si la cuenta es propietaria única de algún Workspace, `422 BUSINESS_RULE_WORKSPACE_OWNERSHIP_UNRESOLVED` y `obligations` los lista. **Reutiliza la guarda de MVP-206**, no la reimplementa |
| Qué desaparece | Nombre, correo e identificador del proveedor de identidad, en la cuenta, en los maestros de responsables de sus Workspaces (RN-036) y en las invitaciones pendientes dirigidas a su correo |
| Qué se conserva | La **fila anonimizada**, porque cada actividad, cosecha y compra guarda quién la registró: borrarla dejaría el histórico operativo de terceros sin autoría. Ya no identifica a nadie |
| Sesiones | Todas las vivas se revocan y la cookie de refresco se borra: sin eso, un token emitido antes seguiría sirviendo |
| Volver a entrar | El `google_sub` deja de coincidir con el de Google, así que entrar con la misma cuenta crea una **cuenta nueva y vacía**. Es lo que hace que la supresión sea de verdad y no una desactivación |
| Retención (CA-5, RN-041) | `purge_after` dice cuándo se purgará físicamente la fila anonimizada: **24 meses**. Se devuelve para que la persona sepa qué queda y hasta cuándo, no solo que «se ha borrado» |
| Irreversible | No hay periodo de gracia ni papelera |

### 0.a ter) Páginas legales (MVP-505)

No son API: son rutas públicas del cliente, listadas aquí porque forman parte del contrato de salida.

| Ruta | Contenido | Enlazada desde |
|---|---|---|
| `/legal/privacidad` | Política de Privacidad | Login, landing y Ajustes |
| `/legal/terminos` | Términos del Servicio | Login, landing y Ajustes |

Sustituyen a los enlaces rotos del login (`P-008`). Son **públicas** a propósito: se leen antes de
entrar, que es cuando hacen falta.

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
| Reemitir invitación (MVP-204 · MVP-208) | `POST /api/v1/workspaces/invitations/{id}/resend` | `deliver_email?` (def. `true`) | `200 { id, channel, email, accept_url, expires_at, email_sent }` |
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
| Reemisión (MVP-204, HU-5/CA-6 · MVP-208, CA-7) | Cualquier invitación **pendiente** del Workspace activo, de **los dos canales**. Rota el token (un solo uso) y renueva la caducidad, igual que la emisión original; la persona sigue `invitado` y el enlace anterior deja de servir. `deliver_email:false` es el reenvío "por enlace" (no reenvía el correo, solo devuelve el nuevo `accept_url`); en el canal `enlace` no hay destinatario, así que `email` es `null` y `email_sent` siempre `false`. Cualquier invitación inexistente, de otro Workspace o no pendiente responde `INVITATION_NOT_FOUND` (404) |
| Anulación (MVP-207, HU-2/CA-4) | Solo invitaciones **pendientes** del Workspace activo, de **cualquier canal** (a diferencia del reenvío: un enlace compartible que se ha ido de las manos es justo el caso en que hace falta retirarlo). Transita a `anulada`: el enlace deja de permitir la aceptación y la persona desaparece de la lista de personas del Workspace. Idempotente en el dominio, pero una segunda llamada responde `INVITATION_NOT_FOUND` (404) porque ya no está pendiente. Cualquier invitación inexistente, de otro Workspace o no pendiente responde igualmente 404 |
| Anular frente a rechazar frente a revocar | `anulada` la fija el **Workspace emisor** sobre quien aún no ha entrado; `rechazada` (MVP-107) la fija la **persona invitada**; revocar (MVP-204, CA-7) retira a quien **ya es miembro**. Una invitación ya aceptada no se anula: se revoca el acceso |
| `viewer.can_accept` / `viewer.reason` (MVP-107) | Aptitud de la cuenta autenticada calculada antes de aceptar; `reason` ∈ `email_mismatch`, `expired`, `already_used`, `already_rejected`, `cancelled`, `already_member`. No revela el email destinatario |
| Bandeja de recibidas (MVP-107) | Solo canal `email` dirigido a la cuenta, pendiente y no caducada; se autoriza por titularidad del email, no por token. No exige Workspace activo |
| Rechazar (MVP-107) | Transita a `rechazada` sin crear membresía; no cierra sesión. Idempotente ante doble rechazo del destinatario |

### 0.c) Telemetría del embudo de login (MVP-105 · MVP-601)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Ingesta de evento de embudo | `POST /api/v1/auth/telemetry/login` | `event*`, `flow_id*`, `session_id`, `device_type` | `202` (sin cuerpo) |

`POST /api/v1/auth/google/callback` acepta además `flow_id`, `session_id` y `device_type`
**opcionales** para correlacionar el éxito/error del intercambio con los eventos de cliente. Si el
`flow_id` no llega, el servidor genera uno.

Validaciones y reglas:

| Regla | Código error / comportamiento |
|---|---|
| `event` dentro de `{ login_screen_viewed, login_google_clicked, login_abandonment }` | `VALIDATION_REQUIRED` (400) si no |
| `flow_id` alfanumérico y de longitud válida (≤ 64) | `VALIDATION_REQUIRED` (400) si no |
| `session_id` alfanumérico y de longitud válida (≤ 64) | Se degrada a `unknown` si no; **no** rechaza el evento |
| `device_type` dentro de `{ desktop, mobile, tablet }` | Se degrada a `unknown` si no; **no** rechaza el evento |
| `login_google_success` / `login_google_error` no se aceptan del cliente | Son autoritativos del servidor (se emiten en el callback) |
| La traza no contiene PII | Solo `event`, `timestamp`, `session_id`, `flow_id`, `channel`, `device_type` y `error_code`; nunca email ni token (RN-020, RN-017) |

Por qué las dimensiones secundarias degradan en vez de rechazar: descartar el evento entero por un
`device_type` mal formado perdería la conversión, que es lo que se quiere medir, y además dejaría al
cliente decidir qué se cuenta con solo mandar un valor inválido.

> El detalle de eventos y campos mínimos del embudo vive en
> `../07-seguridad/autenticacion-autorizacion.md`; cómo se explotan (contadores agregados y ventanas
> de los SLO) en `../05-infraestructura/observabilidad.md`.

### 0.c bis) Errores del intercambio de código con Google (MVP-713)

Cuando `POST /api/v1/auth/google/callback` no puede completar el intercambio, el código de respuesta
depende de **de quién es el error**, según el vocabulario cerrado del endpoint de token de OAuth 2.0
(RFC 6749 §5.2).

| `error` de Google | HTTP | Código de la API | Qué ha pasado |
|---|---|---|---|
| `invalid_grant` | 401 | `AUTH_GOOGLE_CODE_INVALID` | El código de autorización ya se usó o caducó. **Recargar la pantalla de vuelta de Google basta para provocarlo**: el código es de un solo uso |
| `invalid_request` | 400 | `AUTH_GOOGLE_REQUEST_INVALID` | Falta un parámetro del intercambio o viene mal formado. Los tres que aporta el cliente (`code`, `redirect_uri`, `code_verifier`) son suyos |
| `invalid_client`, `unauthorized_client` | 500 | `AUTH_GOOGLE_EXCHANGE_FAILED` | Credenciales o registro de la aplicación mal configurados: es nuestro |
| cualquier otro, o sin `error` legible | 500 | `AUTH_GOOGLE_EXCHANGE_FAILED` | Caída de Google o respuesta que no se entiende |
| — (el intercambio va bien y falla la validación del `id_token`) | 401 | `AUTH_GOOGLE_TOKEN_INVALID` | Sin cambios desde MVP-101 |

Por qué importa el código de estado y no solo el mensaje: **el SLO de tasa de error y la alerta
`HighErrorRate` se calculan sobre las respuestas 5xx**. Hasta `MVP-713`, cualquier respuesta no
exitosa de Google se traducía a `AUTH_GOOGLE_EXCHANGE_FAILED` → 500, así que recargar la pantalla de
callback contaba como fallo del servicio; un solo caso sobre 70 peticiones dio 1,43 % y disparó una
alerta **crítica** con correo real (`MVP-699`, `R-04` · `MVP-999`, `P-079`).

Lo que **no** está clasificado como error de quien llama responde 500 **por defecto**. La dirección
del defecto es deliberada: un fallo propio contado como error de cliente desaparece de las alertas, y
eso es peor que una alerta de más.

### 0.d) Señales de uso del producto (MVP-602)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Ingesta de señal de uso | `POST /api/v1/telemetry/usage` | `event*`, `session_id`, `device_type`, `first_in_session`, `widgets` | `202` (sin cuerpo) |

**Autenticada, sin ámbito de Workspace.** Exigir sesión evita que se inflen los contadores desde
fuera; no exigir Workspace activo es deliberado: una sesión en onboarding también es una sesión
activa, y dejarla fuera del divisor subiría el KPI de uso del dashboard justo con los casos en los que
el producto todavía no sirve de nada.

Validaciones y reglas:

| Regla | Código error / comportamiento |
|---|---|
| `event` dentro de `{ app_session_started, dashboard_viewed, dashboard_manual_refresh, dashboard_widgets }` | `VALIDATION_REQUIRED` (400) si no. `dashboard_manual_refresh` esta **discontinuado desde MVP-706** —el cliente ya no lo emite— pero se sigue aceptando para no responder `400` a un cliente cacheado |
| `session_id` / `device_type` | Se degradan a `unknown`; **no** rechazan la señal |
| `first_in_session` (solo en `dashboard_viewed`) | Ausente equivale a `false`: ante la duda no se infla el numerador del KPI |
| `widgets[].widget` ∈ `{ summary, kg_by_destination, kg_by_plot, yield_evolution, economics }` (`economics` desde MVP-707) y `status` ∈ `{ ok, empty, error }` | Los no reconocidos **se descartan uno a uno**, no el lote: un cliente más nuevo debe seguir aportando lo que el servidor sí conoce |
| `widgets` repetidos | Solo cuenta el primero de cada widget, para que no se pueda inflar la cobertura repitiendo |
| Ningún widget reconocible en `dashboard_widgets` | `VALIDATION_REQUIRED` (400) |
| La señal no contiene PII | Solo `event`, `timestamp`, `session_id`, `channel` y `device_type`. **No lleva usuario ni Workspace**, aunque el endpoint sea autenticado y el servidor los conozca (RN-042) |

### 0.d bis) Canal de sugerencias e incidencias (MVP-711)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Enviar un reporte | `POST /api/v1/feedback` | `kind*`, `message*`, `path`, `last_failed_request_id` | `202` (sin cuerpo) |

**Autenticada, sin ámbito de Workspace.** Autenticada porque el reporte lleva quién lo manda y porque
el límite anti-abuso es por cuenta; sin ámbito de Workspace por el mismo motivo que la baja de cuenta
(`MVP-505`): quien no tiene Workspace activo —o acaba de perderlo— es quien más motivos puede tener
para escribir.

El `202` confirma que **el correo ha salido**, no que alguien lo haya leído: el producto no tiene
estados de reporte y no los finge.

Validaciones y reglas:

| Regla | Código error / comportamiento |
|---|---|
| `kind` dentro del catálogo `feedback_kind` | `VALIDATION_FEEDBACK_KIND_INVALID` (400) |
| `message` no vacío tras recortar espacios | `VALIDATION_REQUIRED_FEEDBACK_MESSAGE` (400) |
| `message` de 2000 caracteres como mucho | `VALIDATION_FEEDBACK_MESSAGE_LENGTH` (400) |
| `path` con forma de ruta del cliente | Se **recorta la query y el fragmento** y se descarta lo que no tenga esa forma; nunca rechaza la petición |
| `last_failed_request_id` con la forma que emite `RequestIdMiddleware` | Se descarta si no la tiene; nunca rechaza la petición |
| Como mucho **3 reportes por hora y cuenta** | `RATE_LIMIT_FEEDBACK` (429) con cabecera `Retry-After`. El cupo se consume **al entregar**, no al intentar |
| Sin `Feedback:Recipient` o sin cuenta de envío | `FEEDBACK_CHANNEL_UNAVAILABLE` (503) |
| El proveedor de correo no acepta el envío | `FEEDBACK_DELIVERY_FAILED` (503). **No se confirma nada**: decir «enviado» sin haber enviado es peor que el fallo |

El contexto técnico que acompaña al reporte **no lo compone el cliente**: la versión desplegada y el
navegador los pone el servidor (ensamblado y cabecera `User-Agent`), y de lo que sí aporta el cliente
solo se acepta lo que tiene forma de serlo. No viaja **nada de la explotación**: ni Workspace, ni
temporada, ni identificadores de registros. Detalle del tratamiento en
[privacidad-datos.md](../07-seguridad/privacidad-datos.md).

### 0.e) Salud y señales operativas (MVP-603)

| Operación | Método y ruta | Autenticación | Respuesta |
|---|---|---|---|
| Comprobación de salud | `GET /api/v1/health` | Anónima | `200` `{ status, database }` · **`503`** si no alcanza la base de datos |
| Señales operativas | `GET /api/v1/ops/signals?days=N` | Llave de servicio `X-Ops-Key` | `200` con el informe · `401` sin llave válida · **`404`** si no hay llave configurada |

`/api/v1/health` es la superficie que sondea la plataforma de alojamiento y la que hace comprobable la
alerta `ServiceDown`. Devuelve **`503`** y no `200` con un cuerpo que diga que va mal: las sondas miran
el código de estado. No expone versión, ni cadena de conexión, ni el motivo del fallo —es una
superficie anónima expuesta a Internet—.

`/api/v1/ops/signals` responde `404` cuando `Ops:ApiKey` no está configurada. Es deliberado: si alguna
vez se despliega sin configurarlo, el fallo debe ser que no se puede consultar, no que lo pueda
consultar cualquiera. La comparación de la llave es en tiempo constante.

El informe agrupa `daily`, `slo`, `login_funnel_7d`, `product_usage_7d`, `business_7d`, `live` (ventana
de 30 min) y `alerts`. Los cocientes son `null` —no `0`— cuando el divisor es cero: «ninguna sesión
abrió el panel» y «no hubo sesiones» no son lo mismo.

`daily` (MVP-699) es la **serie por día**, en orden ascendente y sin huecos: los días sin datos vienen
con recuentos a `0` y cocientes a `null`, porque omitirlos escondería que ese día no se observó nada.

| Parámetro | Efecto |
|---|---|
| `days` | Días de la serie diaria. Por defecto `28`; se acota en silencio a `1..400` y el valor aplicado viaja en `daily_days` |

`days` **no mueve las ventanas de los SLO**: `error_rate_7d` y compañía son de 7 y 30 días porque así
las define la KB, y son parte del objetivo, no una preferencia de consulta. Lo que el parámetro
gobierna es otra pregunta —«¿mejora o empeora?»—, que es la que las ventanas fijas no pueden contestar.

### Ámbito de Workspace en operaciones protegidas (MVP-105)

Toda operación de negocio Workspace-first se marca con `[RequireWorkspaceScope]`: el Workspace activo
se resuelve en servidor desde el claim `workspace_id` (RN-034) y **nunca** viaja como parámetro.

| Regla | Código error |
|---|---|
| La sesión no tiene ningún Workspace activo | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |
| El recurso no pertenece al Workspace activo (`IWorkspaceContext.EnsureInScope`) | `AUTH_WORKSPACE_FORBIDDEN` (403) |

### 0.f) Depuracion de maestros: borrado y fusion (MVP-806)

Los cuatro maestros de `MVP-002` —terrenos, temporadas, trabajadores y tareas— comparten **el mismo
contrato** para las dos operaciones que introduce `RN-037` ampliada. Se documenta una vez aquí y cada
sección de maestro remite a esta; `{maestro}` es `plots`, `seasons`, `workers` o `tasks`.

| Operación | Método y ruta | Request | Respuesta 2xx |
|---|---|---|---|
| Eliminar ficha sin uso | `DELETE /api/v1/{maestro}/{id}` | — | `204` |
| Fusionar dos fichas | `POST /api/v1/{maestro}/{id}/merge` | `absorbed_id*` | `200 { survivor_id, survivor_name, absorbed_id, absorbed_name, reassigned_count }` |

Las dos exigen `[RequireWorkspaceScope]`. El identificador de la **ruta** es siempre el de la ficha
que **sobrevive**; el del cuerpo, el de la que se absorbe y desaparece.

**No llevan `If-Match`**: los maestros no tienen columna de versión. El control de concurrencia de
`ADR-0005` sí actúa, pero sobre los **registros operativos que se reapuntan** en la fusión, no sobre
las fichas.

Además, el **listado** de cada maestro incorpora `usage_count` en cada fila:

| Valor | Significado |
|---|---|
| `0` | Ninguna referencia: la interfaz puede ofrecer «Eliminar» |
| `n > 0` | Hay histórico: no se ofrece el borrado, y es la cifra que anuncia la confirmación de la fusión |
| `null` | **No consultado.** Solo lo trae el listado; el alta y la edición devuelven `null` en vez de `0`, que sería falso en el `PATCH` de una ficha con histórico |

Validaciones y reglas:

| Regla | Código error / comportamiento |
|---|---|
| Ficha inexistente o de otro Workspace (en la **ruta**) | `RESOURCE_NOT_FOUND` (404); `SEASON_NOT_FOUND` (404) en temporadas |
| Ficha absorbida inexistente o de otro Workspace (en el **cuerpo**) | `FOREIGN_KEY_WORKSPACE_MISMATCH` (400): lo que falla es el cuerpo, no la ruta |
| La ficha tiene histórico | `BUSINESS_RULE_MASTER_IN_USE` (422). El mensaje dice **cuántos** registros la referencian y de qué tipo: «No se puede eliminar el terreno «Bancal de arriba»: 2 actividades y 1 cosecha lo referencian» |
| Fusionar una ficha consigo misma | `BUSINESS_RULE_MASTER_MERGE_SELF` (422) |
| La ficha **absorbida** es la de un miembro (`kind: "member"`) | `BUSINESS_RULE_MASTER_MERGE_MEMBER_SURVIVES` (422). Cubre también dos cuentas homónimas |
| Eliminar la ficha de un miembro | `BUSINESS_RULE_WORKER_MEMBERSHIP_MANAGED` (422): la gobierna su acceso, no el maestro |
| Otra persona edita un registro mientras se reapunta | `CONFLICT_VERSION_MISMATCH` (409). La fusión **no se aplica en parte**: o entera o nada |

Qué cuenta como uso, por maestro (declaración única en `MasterReferenceMap`):

| Maestro | Referencias que impiden el borrado |
|---|---|
| `plots` | Actividades, cosechas y **consumos** |
| `seasons` | Actividades, cosechas, **compras** y consumos |
| `workers` | Actividades |
| `tasks` | Actividades que la eligieron del catálogo (`task_id`). La tarea en texto libre de `RN-025` no referencia ninguna fila |

Dos precisiones sobre el recuento:

- **Incluye los registros eliminados lógicamente.** Una actividad con `deleted_at` conserva su clave
  ajena, así que la FK `RESTRICT` seguiría impidiendo el borrado físico. Filtrar por «vivos» daría un
  «sin uso» que la base de datos desmentiría.
- **`workspace_members.active_season_id` no cuenta.** Tener una temporada fijada como temporada de
  trabajo (`MVP-209`) es una preferencia por usuario con `ON DELETE SET NULL`, no histórico: borrarla
  devuelve a esa persona al defecto de `WorkingSeasonPolicy`. En una **fusión** sí se reapunta, para
  que nadie cambie de campaña sin haberlo pedido, pero no suma en `reassigned_count`, que cuenta solo
  registros operativos.

### 1) Plots (terrenos)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta terreno | `POST /api/v1/plots` | `name*`, `ownership_type*`, `alias?`, `owner_name?`, `tree_count?`, `cadastral_reference?`, `location?` | `201 { id, workspace_id, ... }` |
| Editar terreno | `PATCH /api/v1/plots/{plotId}` | campos parciales permitidos | `200 { ...plot }` |
| Listado terrenos | `GET /api/v1/plots` | filtros: `search?`, `is_active?` | `200 { data, meta }` |
| Eliminar terreno sin uso | `DELETE /api/v1/plots/{plotId}` | — | `204` (ver §0.f) |
| Fusionar dos terrenos | `POST /api/v1/plots/{plotId}/merge` | `absorbed_id*` | `200 { ...fusion }` (ver §0.f) |

Validaciones clave. El **alta y la edición no devuelven los mismos códigos** (ver el aviso al final de
esta sección):

| Regla | Alta (`POST`) | Edición (`PATCH`) |
|---|---|---|
| `name` ausente o nulo | `VALIDATION_REQUIRED_NAME` (400) | — (omitirlo mantiene el valor) |
| `name` en blanco | `VALIDATION_REQUIRED_NAME` (400) | `VALIDATION_REQUIRED_NAME` (400) |
| `name` demasiado largo (> 150) | `VALIDATION_PLOT_NAME_LENGTH` (400) | `VALIDATION_PLOT_NAME_LENGTH` (400) |
| `ownership_type` ausente o nulo (RN-028) | `VALIDATION_REQUIRED_PLOT_OWNERSHIP_TYPE` (400) | — (omitirlo mantiene el valor) |
| `ownership_type` en blanco | `VALIDATION_REQUIRED_PLOT_OWNERSHIP_TYPE` (400) | `VALIDATION_REQUIRED_PLOT_OWNERSHIP_TYPE` (400) |
| `ownership_type` fuera de `plot_ownership_type` | `VALIDATION_PLOT_OWNERSHIP_TYPE_INVALID` (400) | `VALIDATION_PLOT_OWNERSHIP_TYPE_INVALID` (400) |
| `tree_count >= 0` (entero) | `VALIDATION_RANGE_TREE_COUNT` (400) | `VALIDATION_RANGE_TREE_COUNT` (400) |
| Alias, propietario, referencia catastral o ubicación demasiado largos | `VALIDATION_PLOT_*_LENGTH` (400) | `VALIDATION_PLOT_*_LENGTH` (400) |
| Nombre ya usado en el Workspace, ignorando mayúsculas (MVP-207) | `CONFLICT_PLOT_NAME_DUPLICATE` (409) | `CONFLICT_PLOT_NAME_DUPLICATE` (409) |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |
| Terreno inexistente o de otro Workspace | — | `RESOURCE_NOT_FOUND` (404) |

Reglas de contexto (MVP-202):

| Regla | Comportamiento |
|---|---|
| Alta mínima (RN-028) | Solo `name` y `ownership_type` son obligatorios; el resto es opcional e informativo |
| `tree_count` ausente | No bloquea; se marca como dato incompleto para el dashboard (RN-010). La respuesta incluye `has_tree_count` |
| Duplicados (MVP-207) | Un Workspace no admite dos terrenos con el mismo `name` ignorando mayúsculas y espacios sobrantes (índice único `(workspace_id, lower(name))`). Los **inactivos también ocupan su nombre**. El `alias` es un apodo libre y **sí** puede repetirse |
| Inactivación con histórico (CA-3) | `PATCH { is_active:false }`; reversible. Un terreno con histórico **no** se elimina |
| Depuración (MVP-806) | El terreno que **nunca** se usó se elimina físicamente y dos que son el mismo se fusionan (§0.f, RN-037). El uso se cuenta contra actividades, cosechas **y consumos**; el listado lo trae en `usage_count` |
| `PATCH` de campos parciales | Un campo ausente mantiene su valor; presente (incluido vacío) lo asigna/limpia |
| `location` | Texto libre. Coordenadas/mapas y `soil_metadata` quedan fuera de alcance del MVP |

> **Los códigos del alta y los de la edición coinciden** desde `MVP-502` (`P-043`, resuelto junto a
> `P-027`; aplica igual a terrenos, temporadas, tareas y trabajadores). Hasta entonces el `POST`
> colapsaba **todo** lo que rechazaba el enlace de modelo en `VALIDATION_REQUIRED` —ausente, en
> blanco y demasiado largo daban lo mismo— mientras el `PATCH` sí emitía el código de dominio, así
> que un cliente no podía saber qué arreglar. Ahora cada anotación declara su propio código y la
> respuesta es la misma por las dos vías.
>
> Dos consecuencias del cambio, que conviene tener presentes al leer las tablas:
>
> - **`VALIDATION_FORMAT_INVALID`** es nuevo y significa «el valor llegó, pero no se puede
>   interpretar»: una fecha que no lo es, un número donde se esperaba un entero, o un cuerpo cuyos
>   bytes no son UTF-8 válido. Se distingue de `VALIDATION_REQUIRED` («falta») a propósito.
> - **Ningún mensaje sale ya en inglés.** Antes, cuando el fallo lo generaba el enlace de modelo, la
>   respuesta arrastraba el texto por defecto de ASP.NET («The request field is required.») y la UI
>   lo mostraba tal cual.

### 2) Seasons (temporadas)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta temporada | `POST /api/v1/seasons` | `name*`, `start_date*`, `end_date?` | `201 { ...season }` (pasa a ser **mi** temporada de trabajo) |
| Editar temporada | `PATCH /api/v1/seasons/{seasonId}` | `name?`, `start_date?`, `end_date?`, `is_closed?` | `200 { ...season }` |
| Listado temporadas | `GET /api/v1/seasons` | — (sin filtros) | `200 { data, meta: { total } }` |
| Mi temporada de trabajo | `GET /api/v1/seasons/active` | — | `200 { ...season }` · `404` si no hay |
| Fijar mi temporada de trabajo | `POST /api/v1/seasons/{seasonId}/activate` | — | `200 { ...season }` |
| Eliminar temporada sin uso | `DELETE /api/v1/seasons/{seasonId}` | — | `204` (ver §0.f) |
| Fusionar dos temporadas | `POST /api/v1/seasons/{seasonId}/merge` | `absorbed_id*` | `200 { ...fusion }` (ver §0.f) |

Todas exigen `[RequireWorkspaceScope]`. La representación de una temporada es
`{ id, workspace_id, name, start_date, end_date, is_working, is_closed, status }` (MVP-209), donde:

- `status` es el valor **derivado** del catálogo `season_status` (`planificada`/`abierta`/`cerrada`), no
  una columna: `cerrada` ≡ `is_closed`; en otro caso `abierta` si `start_date <= hoy` (incluye campañas
  pasadas no cerradas) y `planificada` si `start_date > hoy`. **Independiente** de la temporada de trabajo.
- `is_working` indica si es la temporada de trabajo **del usuario que consulta** (no un flag global): dos
  usuarios del mismo Workspace pueden verla distinta. Es un eje **separado** del estado.

Validaciones clave. El **alta y la edición no devuelven los mismos códigos** (ver el aviso de la
sección de terrenos):

| Regla | Alta (`POST`) | Edición (`PATCH`) |
|---|---|---|
| `name` ausente o nulo | `VALIDATION_REQUIRED_SEASON_NAME` (400) | — (omitirlo mantiene el valor) |
| `name` en blanco | `VALIDATION_REQUIRED_SEASON_NAME` (400) | `VALIDATION_REQUIRED_SEASON_NAME` (400) |
| `name` demasiado largo (> 120) | `VALIDATION_SEASON_NAME_LENGTH` (400) | `VALIDATION_SEASON_NAME_LENGTH` (400) |
| `start_date` ausente | `VALIDATION_REQUIRED` (400) | — (omitirlo mantiene el valor) |
| `start_date` con formato no válido (`YYYY-MM-DD`) | `VALIDATION_FORMAT_INVALID` (400) | `VALIDATION_SEASON_DATE_RANGE` (400) |
| `start_date <= end_date` | `VALIDATION_SEASON_DATE_RANGE` (400) | `VALIDATION_SEASON_DATE_RANGE` (400) |
| Nombre ya usado en el Workspace, ignorando mayúsculas (MVP-207) | `CONFLICT_SEASON_NAME_DUPLICATE` (409) | `CONFLICT_SEASON_NAME_DUPLICATE` (409) |
| Temporada inexistente o de otro Workspace | — | `SEASON_NOT_FOUND` (404) |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |

`POST /seasons/{id}/activate` y `GET /seasons/active` comparten los códigos de la columna de edición:
`SEASON_NOT_FOUND` (404) y `AUTH_WORKSPACE_SCOPE_REQUIRED` (403). Un `start_date` mal formado en el
alta devuelve además el mensaje por defecto de ASP.NET **en inglés**; está registrado en `P-043`.

Reglas de contexto (MVP-201 · MVP-203 · MVP-207 · MVP-209):

| Regla | Comportamiento |
|---|---|
| La temporada creada pasa a ser **mi** temporada de trabajo | Decisión de producto «crear cambia la de trabajo» (P-017), ahora **por usuario** (MVP-209): fija mi `active_season_id`; no toca a otros usuarios ni desbanca nada global. No hay 409 por «ya hay una activa» |
| Temporada de trabajo **por usuario** (RN-022) | Vive en `workspace_members.active_season_id`; puede haber **varias campañas abiertas a la vez**. Sin nada fijado se resuelve el defecto (`WorkingSeasonPolicy`: abierta que contiene hoy → abierta más reciente → más reciente → `null`). Se retira el índice único parcial `ux_seasons_workspace_active` |
| `end_date` es opcional | Fecha de fin **estimada**; no se bloquea por rango operativo (RN-023 es un aviso de las historias operativas, no del maestro) |
| Cierre/reapertura (RN-024) | `PATCH { is_closed:true }` es informativo y no bloquea altas ni ediciones. El estado y la temporada de trabajo son ejes independientes: **fijar como de trabajo una cerrada no la reabre** (MVP-209, CA-4); reabrir es un `PATCH { is_closed:false }` explícito |
| Duplicados (MVP-207) | Un Workspace no admite dos temporadas con el mismo nombre ignorando mayúsculas y espacios sobrantes (índice único `(workspace_id, lower(name))`). Las **cerradas también ocupan su nombre**: cerrar no lo libera |
| `PATCH` de campos parciales | Un campo ausente mantiene su valor. Fijar la temporada de trabajo **no** va aquí: es `POST /seasons/{id}/activate` |
| Orden del listado | Las abiertas primero (por fecha de inicio descendente) y las cerradas al final |
| Borrado y fusión (MVP-806) | Una temporada con histórico se **cierra**, nunca se elimina. La que nunca se usó sí se elimina, y dos que son la misma campaña se fusionan (§0.f, RN-037). Tenerla fijada como temporada de trabajo no cuenta como uso: es una preferencia con `ON DELETE SET NULL` |

### 3) Tasks (tareas)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta tarea | `POST /api/v1/tasks` | `name*`, `is_active?` | `201 { id, workspace_id, name, is_active }` |
| Editar tarea | `PATCH /api/v1/tasks/{taskId}` | `name?`, `is_active?` | `200 { ...task }` |
| Listado tareas | `GET /api/v1/tasks` | `is_active?` | `200 { data, meta }` |
| Eliminar tarea sin uso | `DELETE /api/v1/tasks/{taskId}` | — | `204` (ver §0.f) |
| Fusionar dos tareas | `POST /api/v1/tasks/{taskId}/merge` | `absorbed_id*` | `200 { ...fusion }` (ver §0.f) |

Validaciones clave. El **alta y la edición no devuelven los mismos códigos** (ver el aviso de la
sección de terrenos):

| Regla | Alta (`POST`) | Edición (`PATCH`) |
|---|---|---|
| `name` ausente o nulo | `VALIDATION_REQUIRED_TASK_NAME` (400) | — (omitirlo mantiene el valor) |
| `name` en blanco | `VALIDATION_REQUIRED_TASK_NAME` (400) | `VALIDATION_REQUIRED_TASK_NAME` (400) |
| `name` demasiado largo (> 120) | `VALIDATION_TASK_NAME_LENGTH` (400) | `VALIDATION_TASK_NAME_LENGTH` (400) |
| Nombre ya usado en el Workspace, ignorando mayúsculas | `CONFLICT_TASK_NAME_DUPLICATE` (409) | `CONFLICT_TASK_NAME_DUPLICATE` (409) |
| Tarea inexistente o de otro Workspace | — | `RESOURCE_NOT_FOUND` (404) |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |

Reglas de contexto (MVP-205):

| Regla | Comportamiento |
|---|---|
| Catálogo por Workspace (RN-026) | Arranca **vacío** y es editable por cualquier miembro activo (RN-034). El aislamiento por Workspace lo garantiza `[RequireWorkspaceScope]`: el catálogo de un Workspace no afecta al de otro |
| Duplicados | Un Workspace no admite dos tareas con el mismo nombre ignorando mayúsculas y espacios sobrantes (índice único `(workspace_id, lower(name))`). Las **inactivas también ocupan su nombre**: se reactivan, no se duplican. No hay normalización de acentos: «Poda» y «Podá» conviven |
| Inactivación con histórico (CA-3) | `PATCH { is_active:false }`; reversible. Una tarea con histórico **no** se elimina |
| Depuración (MVP-806) | La tarea que **nunca** se eligió del catálogo se elimina y dos que son la misma labor se fusionan (§0.f, RN-037). La tarea en texto libre de RN-025 no referencia ninguna fila, así que no cuenta como uso |
| `PATCH` de campos parciales | Un campo ausente mantiene su valor |
| Orden del listado | Activas primero y luego por nombre. La operativa diaria pedirá `is_active=true` |
| Tarea en la actividad (RN-025) | La tarea es obligatoria y puede venir del catálogo (`task_id`) o de texto libre (`task_text`) |
| Guardado desde la operativa (MVP-302) | `POST`/`PATCH /api/v1/activities` con `save_task_to_catalog` da de alta aquí la tarea escrita a mano, **reutilizando esta misma guarda de duplicados**: consulta la comparación `lower(name)` para resolver el nombre en vez de chocar con él, así que reutiliza la tarea existente —o reactiva la inactivada— en lugar de devolver `CONFLICT_TASK_NAME_DUPLICATE`. Ese 409 sigue vigente en `POST /tasks`, donde el alta sí es el objetivo |

### 4) Workers (responsables: miembros y cuadrilla)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta de cuadrilla | `POST /api/v1/workers` | `name*`, `hourly_rate?` | `201 { ...worker }` |
| Editar trabajador | `PATCH /api/v1/workers/{workerId}` | `name?`, `hourly_rate?`, `is_active?` | `200 { ...worker }` |
| Listado de responsables | `GET /api/v1/workers` | `is_active?` | `200 { data, meta: { total, members, crew } }` |
| Eliminar cuadrilla sin uso | `DELETE /api/v1/workers/{workerId}` | — | `204` (ver §0.f) |
| Fusionar dos fichas | `POST /api/v1/workers/{workerId}/merge` | `absorbed_id*` | `200 { ...fusion }` (ver §0.f) |

La representación de un responsable es
`{ id, workspace_id, name, hourly_rate, is_active, kind, user_account_id }`, donde `kind` es el
catálogo cerrado `worker_kind` (`member`/`crew`), **derivado** de `user_account_id`, no una columna.

Es el maestro **único** de responsables (MVP-208, CA-1/CA-2): devuelve las dos clases de persona con
un solo espacio de identificadores, que es lo que permite que `ACTIVITY.worker_id` sea una FK simple a
`workers` para cualquiera de ellas.

| `kind` | Quién es | Cómo entra y sale del maestro |
|---|---|---|
| `member` | Miembro del Workspace, con cuenta (`user_account_id` no nulo) | Entra al crearse el Workspace o al aceptarse su invitación (RN-027); sale al revocarse su acceso —a mano o al ceder el Workspace en la baja con copropietarios—, que **inactiva** su fila sin borrarla |
| `crew` | Cuadrilla sin cuenta (`user_account_id` nulo) | Alta, edición e inactivación manuales en este maestro (MVP-204) |

Validaciones clave. El **alta y la edición no devuelven los mismos códigos** (ver el aviso de la
sección de terrenos):

| Regla | Alta (`POST`) | Edición (`PATCH`) |
|---|---|---|
| `name` ausente o nulo | `VALIDATION_REQUIRED_NAME` (400) | — (omitirlo mantiene el valor) |
| `name` en blanco | `VALIDATION_REQUIRED_NAME` (400) | `VALIDATION_REQUIRED_NAME` (400) |
| `name` demasiado largo (> 150) | `VALIDATION_WORKER_NAME_LENGTH` (400) | `VALIDATION_WORKER_NAME_LENGTH` (400) |
| `hourly_rate >= 0` y numérico (opcional, de referencia) | `VALIDATION_RANGE_HOURLY_RATE` (400) | `VALIDATION_RANGE_HOURLY_RATE` (400) |
| Nombre ya usado en el Workspace, ignorando mayúsculas (MVP-207) | `CONFLICT_WORKER_NAME_DUPLICATE` (409) | `CONFLICT_WORKER_NAME_DUPLICATE` (409) |
| Renombrar a un responsable con cuenta (RN-036) | — | `BUSINESS_RULE_WORKER_IDENTITY_MANAGED` (422) |
| Inactivar o reactivar a mano a un responsable con cuenta (RN-027) | — | `BUSINESS_RULE_WORKER_MEMBERSHIP_MANAGED` (422) |
| Trabajador inexistente o de otro Workspace | — | `RESOURCE_NOT_FOUND` (404) |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) | `AUTH_WORKSPACE_SCOPE_REQUIRED` (403) |

Reglas de contexto (MVP-204 · MVP-208):

| Regla | Comportamiento |
|---|---|
| Alcance del maestro (RN-027) | Todas las personas seleccionables como responsables. El `POST` crea siempre `crew`: un miembro entra por su membresía, no por este endpoint |
| Qué se edita de un `member` (CA-4) | Solo `hourly_rate`. Su `name` llega de la identidad de Google (RN-036) y se resincroniza solo cuando cambia allí; su disponibilidad la gobierna la membresía, así que tampoco se inactiva a mano |
| `hourly_rate` | Opcional y de referencia; no automatiza el coste (RN-003). `PATCH { hourly_rate: null }` la limpia. Aplica a las dos clases: es dato operativo del Workspace |
| Duplicados (MVP-207 · MVP-208) | Un Workspace no admite dos responsables con el mismo nombre ignorando mayúsculas y espacios sobrantes (índice único `(workspace_id, lower(name))`), **tampoco cruzando la frontera miembro/cuadrilla**. Los **inactivos también ocupan su nombre**. No hay normalización de acentos: «Perez» y «Pérez» conviven |
| Desempate de nombres (CA-5) | Si el nombre que trae una cuenta ya lo ocupa una fila de cuadrilla, la cuadrilla se renombra con sufijo « (2)» y el miembro conserva el suyo. Si lo ocupa **otro miembro** —dos cuentas homónimas—, el sufijo lo toma quien llega después: ninguno de los dos es renombrable |
| Una fila por cuenta y Workspace (CA-1) | Invariante de datos: índice único parcial `ux_workers_workspace_user_account`. Readmitir a alguien revocado reactiva su fila, no crea una segunda |
| Inactivación con histórico (CA-3) | `PATCH { is_active:false }` en cuadrilla; reversible. Un responsable con histórico **no** se elimina |
| Depuración (MVP-806) | La ficha de cuadrilla que **nunca** se usó se elimina, y dos fichas de la misma persona se fusionan (§0.f, RN-037). La ficha de un `member` ni se elimina ni se absorbe: su nombre lo fija su cuenta (RN-036) y cada cuenta tiene una única fila por Workspace (CA-1). Es la salida del duplicado que dejó el desempate de nombres |
| `PATCH` de campos parciales | Un campo ausente mantiene su valor; presente (incluido vacío) lo asigna/limpia |

### 4.b) Workspace members (personas del Workspace, MVP-204)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Personas del Workspace | `GET /api/v1/workspace-members` | — | `200 { data, meta:{ total, active, invited, revoked } }` |
| Revocar acceso de un miembro | `POST /api/v1/workspace-members/{userId}/revoke` | — | `204` |

`GET /workspace-members` devuelve una **lista unificada** con el estado de membresía
(`worker_member_status`): las membresías reales (`activo`/`revocado`, `kind: "member"`) más las
invitaciones pendientes proyectadas como `invitado` (`kind: "invitation"`). El estado `invitado`
**no** es una fila de `workspace_members`: se combina desde `workspace_invitations`. Orden: activos,
invitados, revocados. Cada entrada incluye señales de UI: `is_self` y `can_revoke` (miembros),
`channel` e `is_expired` (invitaciones).

Desde MVP-208 (CA-7) es la **superficie única de invitaciones pendientes** y proyecta los **dos
canales**: una invitación de canal `enlace` viaja con `email: null` —no tiene destinatario, así que no
es una persona— pero sí es un acceso vivo, y por tanto se puede renovar y anular desde aquí. Antes
solo aparecía en una lista de solo lectura y no había forma de retirarla.

Lo que **ya no** sale de este endpoint son los responsables seleccionables: eso es
`GET /workers` (MVP-208, CA-2). Esta sigue siendo la superficie de **accesos**.

Validaciones y reglas:

| Regla | Código error / comportamiento |
|---|---|
| Cualquier miembro activo puede listar y revocar | Permisos planos en MVP (RN-034) |
| La persona no es un miembro activo del Workspace | `RESOURCE_NOT_FOUND` (404) |
| No se puede revocar al propietario único (CA-8) | `BUSINESS_RULE_CANNOT_REVOKE_OWNER` (422) |
| No se puede revocar al último miembro activo (CA-8) | `BUSINESS_RULE_LAST_ACTIVE_MEMBER` (422) |
| Revocar (CA-7) | La membresía pasa a `revocado`: deja de resolver contexto y de aparecer en el selector (MVP-104), sin borrar el vínculo ni los registros que ese usuario creó. Desde MVP-208 **inactiva además su fila de `workers`**, así que deja de ser seleccionable como responsable sin invalidar lo que ya la referencia |
| Reingreso de un revocado | Por una invitación nueva (MVP-103); no hay reactivación directa. Al aceptarla recupera su fila de responsable, no se duplica |

### 5) Activities (actividades)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta actividad | `POST /api/v1/activities` | `date*`, `plot_id*`, `season_id*`, `worker_id*`, `task_id?`, `task_text?`, `hours*`, `manual_cost*`, `description?`, `save_task_to_catalog?` | `201 { id, version, ...activity }` |
| Editar actividad | `PATCH /api/v1/activities/{activityId}` | campos parciales · `If-Match: <version>` | `200 { ...activity }` |
| Eliminar actividad | `DELETE /api/v1/activities/{activityId}` | `If-Match: <version>` | `204` |
| Listado actividades | `GET /api/v1/activities` | `from?`, `to?`, `plot_id?`, `season_id?`, `worker_id?` | `200 { data, meta: { total } }` |
| Una actividad (MVP-305) | `GET /api/v1/activities/{activityId}` | — | `200 { ...activity }` · `404` |

Todas exigen `[RequireWorkspaceScope]`. La representación de una actividad es
`{ id, workspace_id, date, plot_id, plot_name, season_id, season_name, worker_id, worker_name,
task_id, task_name, task_text, task, hours, manual_cost, description, is_out_of_season_range,
version, created_at, updated_at }` (MVP-301). Los nombres de los maestros llegan **resueltos** en la
misma consulta para que el diario no tenga que pedirlos por separado, y dos campos son **derivados**,
no columnas:

| Campo derivado | Qué es |
|---|---|
| `task` | Texto de la tarea venga del catálogo o del campo libre (RN-025), para que ningún cliente rehaga ese `??` |
| `is_out_of_season_range` | `true` si la fecha cae fuera del rango de la temporada asociada. Es el **aviso** de RN-023, nunca un bloqueo; se calcula en lectura, así que sigue siendo correcto si la temporada se edita después |
| `task_catalog_outcome` | MVP-302 — qué pasó en el catálogo al pedir `save_task_to_catalog`: `created`, `reused` o `reactivated`. `null` en las lecturas y cuando no se pidió |

Validaciones clave:

| Regla | Código error |
|---|---|
| terreno, temporada y responsable obligatorios | `VALIDATION_ACTIVITY_REQUIRED_FIELDS` (400) |
| tarea obligatoria por catálogo o texto libre, y **no las dos** | `VALIDATION_ACTIVITY_TASK_REQUIRED` (400) |
| `task_text` de longitud válida (≤ 120, la del catálogo) | `VALIDATION_ACTIVITY_TASK_TEXT_LENGTH` (400) |
| `hours > 0` (y ≤ 999,99 por `decimal(5,2)`) | `VALIDATION_ACTIVITY_HOURS_RANGE` (400) |
| `manual_cost >= 0` (0 es válido: labor propia sin coste imputado) | `VALIDATION_ACTIVITY_COST_RANGE` (400) |
| `description` de longitud válida (≤ 500) | `VALIDATION_ACTIVITY_DESCRIPTION_LENGTH` (400) |
| `save_task_to_catalog` sobre una tarea que **ya** viene del catálogo (MVP-302) | `VALIDATION_ACTIVITY_TASK_NOT_FREE_TEXT` (400) |
| Integridad de workspace en FKs | `FOREIGN_KEY_WORKSPACE_MISMATCH` (400) |
| `PATCH`/`DELETE` sin cabecera `If-Match` (ADR-0005) | `VALIDATION_REQUIRED_IF_MATCH` (400) |
| Actividad inexistente, de otro Workspace o ya eliminada | `RESOURCE_NOT_FOUND` (404) |
| Edición o borrado con versión desfasada (ADR-0005) | `CONFLICT_VERSION_MISMATCH` (409) |
| `from`/`to` con formato distinto de `YYYY-MM-DD` | `VALIDATION_REQUIRED` (400) |

Reglas de contexto (MVP-301):

| Regla | Comportamiento |
|---|---|
| `worker_id` (P-034) | Es un `workers.id` cualquiera de `GET /api/v1/workers`, sin distinguir clase: desde MVP-208 los miembros del Workspace también son filas de ese maestro, así que no hacen falta campos alternativos ni un responsable polimórfico |
| Tarea (RN-025) | `task_id` **o** `task_text`, exactamente uno. En el `PATCH`, si viene **cualquiera** de los dos se sustituye la pareja completa y el ausente pasa a nulo: enviar solo `task_id` sobre una actividad con texto libre dejaría los dos informados y el dominio lo rechazaría |
| Coste (RN-003) | Siempre manual. El servidor no lo calcula ni lo recalcula; `workers.hourly_rate` solo permite a la UI **sugerir** un valor que la persona confirma |
| Fecha fuera de rango (RN-023) | Nunca bloquea el guardado: se responde `201`/`200` con `is_out_of_season_range: true` |
| Maestros inactivos | Siguen siendo referenciables. La UI ofrece solo los activos para registros nuevos (CA-3 de MVP-202/204/205), pero corregir una actividad que referencia un maestro ya inactivado no obliga a reactivarlo |
| Orden del listado | Fecha de negocio descendente (RN-033) y, a igualdad de fecha, fecha de captura descendente. Sin paginación en el MVP (`MVP-999`, P-051) |
| Precisión | `hours` y `manual_cost` se redondean a 2 decimales en el dominio (`decimal(5,2)` y `decimal(10,2)`), para que lo leído coincida con lo escrito |
| `save_task_to_catalog` (MVP-302) | Guarda `task_text` en el catálogo del Workspace y deja la actividad referenciándolo por `task_id`, en la **misma transacción**. Si el nombre ya existe se **reutiliza** (y si estaba inactivada, se **reactiva**, MVP-205 CA-3): este flujo nunca devuelve `CONFLICT_TASK_NAME_DUPLICATE`, porque un 409 aquí no sería accionable. Los errores de nombre son los **del catálogo** (`VALIDATION_REQUIRED_TASK_NAME`, `VALIDATION_TASK_NAME_LENGTH`) |
| `PATCH { save_task_to_catalog: true }` a secas (MVP-302) | Promociona el `task_text` que la actividad **ya tiene**, sin reescribirlo. Es la vía para guardar en el catálogo la tarea de una actividad ya registrada; la versión sube una sola vez |

> **Formato de `If-Match`** (MVP-301). El contrato publica la versión como el entero `version` de la
> respuesta, pero un cliente HTTP correcto puede enviarla como **ETag**, así que se aceptan las tres
> formas: `3`, `"3"` y `W/"3"`. Se rechaza `*` —significa «cualquier versión», que es justo lo que el
> bloqueo optimista existe para impedir— con el mismo `400 VALIDATION_REQUIRED_IF_MATCH` que la
> cabecera ausente. El `409` incluye además `current_version` en el cuerpo, para que el cliente pueda
> resolver el conflicto refrescando en vez de dejar al usuario sin salida.
>
> **Concurrencia y borrado de los registros operativos** (aplica igual a actividades, cosechas,
> compras e imputaciones). Las tres entidades operativas son las **entidades críticas** de `ADR-0005`:
> exponen `version`, exigen `If-Match` en `PATCH`/`DELETE` y responden `409 CONFLICT_VERSION_MISMATCH`
> si la versión enviada no es la vigente. El `DELETE` es una **baja lógica** (`deleted_at`, RN-037):
> el registro desaparece del diario, de los listados y del dashboard, pero no se borra físicamente, y
> la UI exige confirmación explícita antes de invocarlo. No hay papelera ni restauración en el MVP.
> Alcance de implementación: `MVP-301`/`MVP-303`/`MVP-304` para actividades, compras e imputaciones, y
> `MVP-401` para cosechas —**las cuatro implementadas**—.

### 5.b) Diary (diario cronológico unificado, MVP-305 · MVP-506)

| Operación | Método y ruta | Request (query) | Respuesta 2xx |
|---|---|---|---|
| Diario del Workspace | `GET /api/v1/diary` | `from?`, `to?`, `plot_id?`, `season_id?` (id \| `all`), `type?` (repetible), `worker_id?`, `search?`, `page?`, `limit?` | `200 { data, meta }` |

> **MVP-701 — Ámbito de temporada (RN-008).** Sin `season_id` se aplica la **temporada de trabajo del
> usuario**, no «todas»: hasta esta historia solo el dashboard resolvía el defecto y estas listas
> arrancaban sin acotar, de modo que dos pantallas daban totales distintos de la misma campaña
> (`P-082`). El histórico completo se pide con **`season_id=all`**, y el ámbito aplicado viaja en
> `meta.scope` = `{ season: { id, name, status, start_date, end_date } | null, all_seasons }`. Un
> `season_id` inexistente o de otro Workspace **cae al defecto**, con el mismo criterio de tolerancia
> que el dashboard aplica a `plot_ids`.

Es **la vista principal del MVP** (RN-033) y es de **solo lectura**: cada registro se crea, corrige y
elimina por el recurso al que pertenece (`/activities`, `/purchases`, `/consumptions`), que es donde
viven sus reglas. El diario únicamente agrega.

La entrada del diario es una **proyección común** de las cuatro entidades operativas:
`{ type, id, date, title, description, plot_id, plot_name, season_id, season_name, cost, version,
is_out_of_season_range, created_at, worker_name, hours, task_id, quantity, has_purchase, kgs,
destination, yield }`. Los campos específicos de un tipo llegan a `null` en los demás.

| Campo | Por qué está |
|---|---|
| `version` | Permite eliminar desde el diario con `If-Match` (ADR-0005) sin abrir antes el registro |
| `task_id` | Solo en actividades: `null` ⇒ tarea escrita a mano, lo que permite ofrecer guardarla en el catálogo (MVP-302) |
| `has_purchase` | Solo en consumos: `false` ⇒ el coste es desconocido, no cero (RN-032) |
| `kgs` / `destination` | Solo en cosechas (MVP-401). Van aparte de `quantity` porque no son la misma magnitud: una cosecha se lee en kilos y la tarjeta la rotula distinto |
| `yield` | Solo en cosechas (MVP-402): el rendimiento **efectivo** en L/100kg, declarado o derivado de los litros (RN-013/RN-014) |
| `amount` | Solo en cosechas (MVP-707): importe ingresado (`kgs × unit_price`). `null` cuando la partida no tiene precio |

`meta` es
`{ scope, total, page, limit, total_cost, imputed_cost, activities, purchases, consumptions, consumptions_without_purchase, harvests, total_kg, total_income, harvests_with_price }`:

| Campo de `meta` | Qué mide |
|---|---|
| `total` · `page` · `limit` | MVP-506 — posición dentro del conjunto. **`total` es el del diario filtrado completo, no el de la página**: es lo que permite saber cuántas páginas hay. El resto de contadores e importes también son del conjunto, porque son la cabecera del muro y cambiarían en cada avance si contaran solo lo visible |
| `total_cost` | **Gasto real** de lo que se está viendo: labores + compras + consumos **sin compra**. **No** incluye las imputaciones: reparten dinero que la compra ya aportó, así que sumarlas contaría el mismo gasto dos veces (`MVP-399`, hallazgo `R-01`). Es el criterio que debe heredar el dashboard de `MVP-004` |
| `imputed_cost` | Lo repartido por terrenos: **desglose** de `total_cost`, no gasto añadido |
| `consumptions_without_purchase` | Consumos sin compra previa. Su coste consta como `0` porque se desconoce (RN-032), así que el gasto real fue algo mayor; la UI lo advierte (CA-3 de `MVP-003`) |
| `harvests` / `total_kg` | Cosechas y kilos recolectados de lo filtrado (MVP-401). La cosecha **no aporta gasto** (RN-029), así que se resume por kilos: es su magnitud |
| `total_income` / `harvests_with_price` | **MVP-707** — Ingreso de lo filtrado (`kgs × unit_price` de las cosechas que lo tienen) y sobre cuántas partidas se ha sumado. Va **aparte** de `total_cost`: son magnitudes distintas y mezclarlas obligaría a un signo, que cada consumidor puede leer al revés. `total_income` a `null` significa que ninguna partida tiene precio, que no es lo mismo que 0 € |

| Catálogo | Valores permitidos |
|---|---|
| `diary_entry_type` | `actividad`, `compra`, `consumo`, `cosecha` (los cuatro vivos desde `MVP-401`) |

Reglas de contexto:

| Regla | Comportamiento |
|---|---|
| Orden | Fecha de **negocio** descendente (RN-033) y, a igualdad, fecha de captura descendente |
| Filtro `type` | Ahorra trabajo, no solo oculta: los tipos no pedidos ni se consultan |
| Filtro `plot_id` | Deja fuera las **compras** por definición: una compra es del Workspace y solo se reparte por terrenos al imputarla (MVP-304). El cliente lo explica para que no parezca un fallo. Las **cosechas sí se conservan**: una cosecha es de un terreno (RN-001) |
| Filtro `worker_id` (MVP-506) | Deja fuera **cosechas, compras y consumos**, por el mismo motivo que `plot_id` deja fuera las compras: no tienen responsable. El cliente lo explica. Combinarlo con `type` de un tipo sin responsable devuelve vacío, que es la respuesta honesta |
| `search` (MVP-506) | Texto libre, sin distinguir mayúsculas, sobre titular, terreno, responsable y descripción —cada tipo busca en los campos que tiene—. Se resuelve **en servidor**: sobre una vista paginada, buscar solo en lo visible daría un resultado falso |
| `type=cosecha` | Vivo desde `MVP-401`, que es quien crea `HARVEST` (hallazgo `G-4`). Con los cuatro tipos, `RN-033` queda cumplida entera |
| `cost` de una cosecha | Siempre `0`: la cosecha **no tiene coste** (RN-029, que deja fuera precio, molturación y balance). No es «gratis» ni «desconocido»: la magnitud no aplica, y por eso la tarjeta muestra kilos donde las demás muestran dinero |
| Paginación (MVP-506) | `page` (def. `1`) y `limit` (def. `20`, **acotado a `100`**). Pedir más del máximo no es un error del cliente, pero servirlo sí sería un problema del servidor: se acota en silencio y `meta.limit` dice lo que se aplicó. `page` o `limit` no positivos responden `400 VALIDATION_FORMAT_INVALID` |
| Mezcla en SQL (MVP-506) | Los cuatro tipos se unen con `UNION ALL` y la base de datos resuelve orden, página y totales. Antes se mezclaban **en memoria** sobre los cuatro listados: equivalente mientras no había paginación, pero paginar sobre cuatro listas ya materializadas no es paginar (`P-051`) |
| Estabilidad de la paginación | El orden desempata por `id` tras fecha de negocio y fecha de captura: sin ese tercer criterio, dos entradas del mismo instante pueden repetirse o perderse entre páginas |

### 6) Harvests (cosechas)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta cosecha | `POST /api/v1/harvests` | `date*`, `plot_id*`, `season_id*`, `product*`, `kgs*`, `destination*`, `yield?`, `liters?`, `yield_unit?`, `unit_price?` | `201 { id, version, ...harvest }` |
| Editar cosecha | `PATCH /api/v1/harvests/{harvestId}` | campos parciales · `If-Match: <version>` | `200 { ...harvest }` |
| Eliminar cosecha | `DELETE /api/v1/harvests/{harvestId}` | `If-Match: <version>` | `204` |
| Listado cosechas | `GET /api/v1/harvests` | `from?`, `to?`, `plot_id?`, `season_id?` (id \| `all`), `destination?` | `200 { data, meta: { scope, total, total_kg } }` |
| Una cosecha (MVP-401) | `GET /api/v1/harvests/{harvestId}` | — | `200 { ...harvest }` |
| Partidas iguales (MVP-805) | `GET /api/v1/harvests/duplicates` | `plot_id*`, `date*`, `product*`, `exclude_id?` | `200 { data:[{ id, kgs, destination }], meta:{ total } }` |

> **MVP-707 — `unit_price` y `amount` (RN-029 matizada).** `unit_price` es el precio de venta por kilo,
> **opcional**; `amount` es su importe (`kgs × unit_price`) y es **derivado, no columna**: no se envía
> nunca y no se persiste, porque guardarlo permitiría que divergiera de sus factores al corregir los
> kilos. Los dos llegan a `null` cuando no hay precio, y `null` significa **no se sabe**, no cero: una
> partida sin precio no ha ingresado 0 €. En `PATCH`, un `unit_price: null` explícito **retira** el
> precio de una partida que lo tenía. Un `unit_price` de `0` o negativo se rechaza con
> `VALIDATION_HARVEST_UNIT_PRICE_RANGE` (400): quien no lo sepa deja el campo vacío.
>
> **MVP-805 — `duplicates` (RN-044, `RU-24`).** Lectura de apoyo del formulario de cosecha: devuelve
> las partidas **vivas** del Workspace con el mismo terreno, la misma fecha y el mismo producto, para
> pintar un **aviso no bloqueante** mientras se rellena. Los tres parámetros son obligatorios y su
> ausencia es `400 VALIDATION_REQUIRED`: responder «no hay duplicados» sin haber podido comprobarlo
> haría que el formulario mostrase silencio donde no hay respuesta. `exclude_id` es la propia partida
> al corregir (CA-3 de `MVP-805`). Devuelve solo lo que el aviso necesita **nombrar** —kilos y
> destino—, no la cosecha entera. Tiene ruta propia y no parámetros de `GET /harvests` para que la
> regla de qué cuenta como duplicado tenga un nombre en el contrato, en vez de vivir repartida en los
> dos formularios que la usan.

La representación de una cosecha es
`{ id, workspace_id, date, plot_id, plot_name, season_id, season_name, product, kgs, yield, liters,
effective_yield, yield_source, destination, is_out_of_season_range, version, created_at, updated_at }`
(MVP-401, ampliada en MVP-402). `plot_name` y `season_name` llegan resueltos y
`is_out_of_season_range` es el mismo aviso derivado de RN-023 que en la actividad y la compra.
`meta.total_kg` del listado son los **kilos acumulados de lo filtrado**, calculados en servidor, con el
mismo criterio que `meta.total_cost` en compras.

| Campo (MVP-402) | Qué es |
|---|---|
| `yield` | Rendimiento **informado**, siempre en la unidad canónica L/100kg (RN-013), sea cual sea la unidad en la que se escribió |
| `effective_yield` | Rendimiento **venga de donde venga**: el informado, o el derivado de `liters / kgs × 100` cuando lo declarado fueron litros (RN-014, tercer origen). Es lo que hace que la exclusión de RN-004 no cueste información: el dashboard promedia también las partidas que declararon litros |
| `yield_source` | `informado`, `calculado` o `null`. Derivado; permite que la UI no presente como declarado un valor deducido |

Validaciones clave:

| Regla | Código error |
|---|---|
| `product` obligatorio y dentro de catálogo cerrado | `VALIDATION_PRODUCT_INVALID` |
| `kgs` obligatorio y > 0 | `VALIDATION_HARVEST_KGS_REQUIRED` |
| `yield` y `liters` no pueden coexistir | `VALIDATION_HARVEST_XOR_YIELD_LITERS` |
| `yield` fuera de rango (0 < `yield` ≤ 100 L/100kg, ya convertido) o `liters` ≤ 0 | `VALIDATION_HARVEST_YIELD_RANGE` / `VALIDATION_HARVEST_LITERS_RANGE` (400) |
| `yield_unit` fuera del catálogo (MVP-402) | `VALIDATION_HARVEST_YIELD_UNIT_INVALID` (400) |
| destino en catálogo cerrado | `VALIDATION_DESTINATION_INVALID` |
| Terreno o temporada ausentes | `VALIDATION_HARVEST_REQUIRED_FIELDS` (400) |
| Terreno o temporada de otro Workspace | `FOREIGN_KEY_WORKSPACE_MISMATCH` (400) |
| `PATCH`/`DELETE` sin cabecera `If-Match` (ADR-0005) | `VALIDATION_REQUIRED_IF_MATCH` (400) |
| Cosecha inexistente, de otro Workspace o ya eliminada | `RESOURCE_NOT_FOUND` (404) |
| Edición o borrado con versión desfasada (ADR-0005) | `CONFLICT_VERSION_MISMATCH` (409) |

**El alta (`POST`) y la edición (`PATCH`) no devuelven los mismos códigos** (mismo patrón que el resto
de recursos; ver el aviso de la sección de terrenos, MVP-499/R-04). Los códigos de dominio de la tabla
son los del **alta**. En el `PATCH`, un campo con **tipo mal formado** —`date` no `YYYY-MM-DD`,
`plot_id`/`season_id` no-UUID, `kgs`/`yield`/`liters` no numéricos, `yield_unit` no-cadena— se rechaza
en el borde con el genérico `VALIDATION_REQUIRED` (400), no con el código de dominio específico. Además,
en el **alta** un `date` ausente o mal formado responde `VALIDATION_HARVEST_REQUIRED_FIELDS` (400), el
mismo código que «terreno o temporada ausentes». Y un `from`/`to` mal formado en `GET /harvests`
responde `VALIDATION_REQUIRED` (400), como en el diario (MVP-499/R-05).

Reglas de contexto (MVP-401):

| Regla | Comportamiento |
|---|---|
| Par `yield`/`liters` en el `PATCH` | Si viene **cualquiera** de los dos se sustituye la pareja completa y el ausente pasa a nulo. Enviar solo `liters` sobre una cosecha con `yield` dejaría los dos informados y el dominio lo rechazaría: es el mismo criterio que el par tarea de la actividad (§5) |
| Retirar el dato de aceite | `yield: null` o `liters: null` explícitos lo dejan sin informar, que es un estado válido (RN-004: los dos son opcionales) |
| Fecha fuera de rango (RN-023) | Nunca bloquea el guardado: se responde `201`/`200` con `is_out_of_season_range: true` |
| Maestros inactivos | Siguen siendo referenciables, igual que en actividades: inactivar deja de ofrecer, no invalida el histórico |
| Orden del listado | Fecha de negocio descendente (RN-033) y, a igualdad de fecha, fecha de captura descendente. Sin paginación en el MVP (`MVP-999`, `P-051`) |
| Filtro `destination` | Comparación **exacta**: es un catálogo cerrado (RN-012), no texto libre como el material de compra (RN-031) |
| Precisión | `kgs` y `liters` se redondean a 2 decimales y `yield` a 4, para que lo leído coincida con lo escrito |
| Catálogos cerrados (MVP-402) | El **servidor es la autoridad**: producto y destino se validan por pertenencia y el `400` incluye los valores admitidos en el mensaje. `desconocido` es el canon del destino no clasificado; «Sin destino» es solo alias visual (RN-012) y se **rechaza** como valor |
| `yield_unit` (MVP-402) | Unidad del `yield` **de esta petición** (RN-014). Ausente ⇒ canónica. No es un campo del recurso, así que en el `PATCH` no «conserva» nada: un `PATCH` que no toca el rendimiento nunca reconvierte lo ya persistido |
| Rendimiento «calculado» de RN-014 | No se persiste: es `effective_yield`, derivado en lectura. Guardarlo duplicaría un dato implícito que quedaría obsoleto al corregir los kilos |
| Densidad de RN-016 | Constante única `0,92 kg/L`. El override por almazara que la regla contempla queda fuera del MVP —no existe la entidad— y está registrado en `MVP-999` (`P-061`) |

La cosecha se registra por fecha, así que **también entra en el diario cronológico** de `MVP-305`
(RN-033). La enciende `MVP-401`, que es quien crea `HARVEST`: con los cuatro tipos vivos, RN-033 queda
cumplida entera (hallazgo `G-4`).

### 7) Purchases (compras)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta compra | `POST /api/v1/purchases` | `purchase_date*`, `product*`, `total_quantity*`, `total_cost*`, `season_id*` | `201 { id, version, unit_price, ... }` |
| Editar compra | `PATCH /api/v1/purchases/{purchaseId}` | campos parciales · `If-Match: <version>` | `200 { ...purchase }` |
| Eliminar compra | `DELETE /api/v1/purchases/{purchaseId}` | `If-Match: <version>` | `204` |
| Listado compras | `GET /api/v1/purchases` | `product?`, `season_id?` (id \| `all`), `from?`, `to?` | `200 { data, meta: { scope, total, total_cost } }` |
| Materiales del histórico (MVP-303 · MVP-708) | `GET /api/v1/purchases/products` | `search?` | `200 { data:[{ product, times_used }], meta:{ total } }` |
| Imputar compra a terreno | `POST /api/v1/purchases/{purchaseId}/consumptions` | `date*`, `plot_id*`, `quantity*` | `201 { id, purchase_id, plot_id, date, quantity, proportional_cost }` |
| Registrar consumo **sin compra previa** (RN-032) | `POST /api/v1/consumptions` | `date*`, `plot_id*`, `season_id*`, `product*`, `quantity*` | `201 { id, purchase_id: null, proportional_cost: 0, ... }` |
| Listado de consumos | `GET /api/v1/consumptions` | `from?`, `to?`, `plot_id?`, `season_id?` (id \| `all`), `purchase_id?`, `product?` | `200 { data, meta: { scope, total, total_cost, without_purchase } }` |
| Editar consumo (MVP-304) | `PATCH /api/v1/consumptions/{consumptionId}` | campos parciales · `If-Match: <version>` | `200 { ...consumption }` |
| Eliminar consumo (MVP-304) | `DELETE /api/v1/consumptions/{consumptionId}` | `If-Match: <version>` | `204` |

La representación de un consumo es
`{ id, workspace_id, purchase_id, has_purchase, purchase_date, plot_id, plot_name, season_id,
season_name, date, product, quantity, unit_price, proportional_cost, is_out_of_season_range,
is_before_purchase_date, version, created_at, updated_at }` (MVP-304 · MVP-708). `has_purchase` es
**derivado** y desambigua el coste: `proportional_cost: 0` con `has_purchase: false` significa «se
desconoce», no «fue gratis». `meta.without_purchase` cuenta esos registros: es la medida del impacto
en la calidad del dato que pide el CA-3 de `MVP-003`.

`purchase_date` e `is_before_purchase_date` los añade `MVP-708` (RN-043) y son **derivados** de la
compra tal y como está ahora, no columnas del consumo: `null` y `false` respectivamente cuando no hay
compra previa. La fecha viaja para que el formulario pueda avisar mientras se teclea sin tener que
pedir la compra aparte.

La representación de una compra es
`{ id, workspace_id, purchase_date, season_id, season_name, product, total_quantity, total_cost,
unit_price, is_out_of_season_range, version, created_at, updated_at }` (MVP-303). `season_name` llega
resuelto y `is_out_of_season_range` es el mismo aviso derivado de RN-023 que en la actividad.
`meta.total_cost` del listado es el **gasto acumulado de lo filtrado**, calculado en servidor.

Validaciones clave:

| Regla | Código error |
|---|---|
| `product` obligatorio (texto libre, RN-031) | `VALIDATION_PURCHASE_REQUIRED_PRODUCT` (400) |
| `product` de longitud válida (≤ 150) | `VALIDATION_PURCHASE_PRODUCT_LENGTH` (400) |
| `season_id` obligatorio (RN-021) | `VALIDATION_PURCHASE_REQUIRED_FIELDS` (400) |
| `total_quantity > 0` y `total_cost > 0` | `VALIDATION_PURCHASE_TOTALS_RANGE` (400) |
| La temporada no existe en el Workspace activo | `FOREIGN_KEY_WORKSPACE_MISMATCH` (400) |
| `PATCH`/`DELETE` sin cabecera `If-Match` (ADR-0005) | `VALIDATION_REQUIRED_IF_MATCH` (400) |
| Compra inexistente, de otro Workspace o ya eliminada | `RESOURCE_NOT_FOUND` (404) |
| suma imputaciones <= cantidad total | `VALIDATION_CONSUMPTION_OVERFLOW` (400) |
| `quantity > 0` en la imputación y en el consumo | `VALIDATION_CONSUMPTION_QUANTITY_RANGE` (400) |
| `product` obligatorio en el consumo sin compra (RN-031) | `VALIDATION_CONSUMPTION_REQUIRED_PRODUCT` (400) |
| Terreno o temporada ausentes o de otro Workspace | `VALIDATION_CONSUMPTION_REQUIRED_FIELDS` / `FOREIGN_KEY_WORKSPACE_MISMATCH` (400) |
| Imputar sobre una compra inexistente, ajena o eliminada | `RESOURCE_NOT_FOUND` (404) |
| Dar de baja una compra con imputaciones vivas (MVP-304) | `BUSINESS_RULE_PURCHASE_HAS_CONSUMPTIONS` (422) |
| Edición o borrado con versión desfasada (ADR-0005) | `CONFLICT_VERSION_MISMATCH` (409) |

Reglas de contexto de consumos (MVP-304):

| Regla | Comportamiento |
|---|---|
| Una sola entidad para los dos casos | Una imputación y un consumo sin compra son el **mismo hecho**; lo único que cambia es de dónde sale el coste. `purchase_id` anulable, decidido en `MVP-303` antes de cerrar el modelo de compras |
| Qué se hereda al imputar | `product`, `season_id` y `unit_price` los pone la compra; el usuario solo elige terreno, fecha y cantidad. La temporada no es cambiable al imputar: desalinearía el reparto respecto del gasto |
| Precio **congelado** (RN-032, CA-3) | El consumo guarda su propio `unit_price`. Editar la compra después **no** reescribe el coste de lo ya consumido, y un consumo sin compra **no** gana coste porque aparezca luego una compra del mismo material: no hay emparejamiento por nombre en ninguna parte |
| Guarda de sobre-imputación | Suma solo las imputaciones **vivas**, así que retirar una libera su cantidad. El reparto **exacto** del 100% se admite; el mensaje del 400 dice cuánto queda por repartir. Al editar se excluye la propia fila |
| Baja de una compra con imputaciones | Se rechaza con 422 indicando cuántas hay. Ni cascada —borraría registros operativos del diario que nadie pidió borrar— ni huérfanas —perderían el origen de su coste—: se retiran primero |
| `imputed_quantity` / `pending_quantity` | El listado de compras los incluye para poder mostrar «imputado / total» sin una consulta por fila |
| Orden del listado de consumos | Fecha de **negocio** descendente (RN-033, CA-4): un consumo capturado hoy sobre trabajo de la semana pasada se lee donde ocurrió |
| Filtro `product` (MVP-399) | Búsqueda **parcial** e insensible a mayúsculas, igual que en compras. Añadido en la revisión de cierre (`R-06`): el buscador de material del libro filtraba las compras y dejaba los consumos intactos |
| Consumo anterior a su compra (MVP-708, RN-043) | Se admite y responde `201`: la captura retroactiva es legítima y `RN-032` ya asume que el papeleo va por detrás del campo. Se **avisa** —señal en el formulario y etiqueta en la fila— con la misma filosofía que `RN-023` usa para la temporada (`P-058`). La igualdad de fechas no avisa: comprar y gastar el mismo día es lo normal |

Reglas de contexto (MVP-303):

| Regla | Comportamiento |
|---|---|
| Producto en texto libre (RN-031) | No hay catálogo cerrado ni normalización: «Abono NPK» y «abono npk» conviven. `GET /purchases/products` devuelve el vocabulario **aprendido del histórico vivo**, los más usados primero y con tope de 20; es una ayuda de escritura, no un maestro |
| Alcance del vocabulario (MVP-708) | Desde `MVP-708` (`P-057`) el vocabulario sale de los **dos** libros: compras y consumos **sin compra previa**. Las imputaciones no cuentan —copian el material de su compra, así que no pueden aportar un nombre nuevo y solo inflarían `times_used`—. La ruta sigue bajo `/purchases` porque es la contratada; lo que cambió es de dónde se aprende, no qué se pide |
| Filtro `product` | Búsqueda **parcial** e insensible a mayúsculas: el texto libre obligaría, si no, a recordar cómo se escribió |
| `unit_price` | Derivado de `total_cost / total_quantity` con 4 decimales y **persistido**. Es la base del coste proporcional de las imputaciones (`MVP-304`) y permite explicar una imputación antigua aunque la compra se edite después (RN-032). Se recalcula en cada `PATCH` que toque cantidad o coste |
| Temporadas cerradas | Siguen admitiendo compras: cerrar es informativo (RN-024) |
| Lo eliminado deja de sugerirse | Una compra dada de baja sale del listado, del `total_cost` y de las sugerencias (RN-037) |

> **El consumo sin compra previa necesita sitio propio** (`MVP-299`, 3ª pasada, hallazgo `G-2`).
> `RN-032` y el CA-3 de la épica `MVP-003` obligan a que la ausencia de compra **nunca** bloquee el
> registro del consumo, pero hasta esta revisión la única ruta contratada colgaba de una compra
> (`POST /purchases/{id}/consumptions`) y el ER declaraba `purchase_id` obligatorio: la excepción más
> importante de la épica no tenía dónde vivir. Requisitos que el modelo debe cumplir, cerrados aquí;
> **el mecanismo concreto lo elige el `tech-design` de `MVP-304`** (columna `purchase_id` anulable
> sobre la entidad existente frente a entidad de consumo propia):
>
> - `purchase_id` es **opcional**. Sin compra, `proportional_cost` es `0` y la respuesta lo señala
>   para que la UI pueda avisar (RN-032). Registrar la compra después **no** recalcula lo ya guardado.
> - El consumo tiene **fecha propia** (`date`) y **temporada** (`season_id`), no solo `created_at`
>   (hallazgo `G-3`): el diario lo ordena por fecha de negocio junto a actividades y compras (RN-033)
>   y `RN-021` exige temporada en toda la operativa. Al imputar sobre una compra, la temporada se
>   hereda de ella.
> - Sin compra previa hace falta `product` (texto libre, RN-031) porque no hay compra de la que
>   heredarlo.

### 8) Dashboard

| Operación | Método y ruta | Request (query) | Respuesta 2xx |
|---|---|---|---|
| Resumen temporada | `GET /api/v1/dashboard/summary` | `season_id?`, `plot_ids?[]` | `200 { scope, total_kg, total_liters, average_yield, harvests, harvests_with_oil_data, kg_per_tree, trees_counted, plots_counted, plots_without_tree_count }` |
| Kg por destino | `GET /api/v1/dashboard/kg-by-destination` | `season_id?`, `plot_ids?[]` | `200 { scope, data:[{ destination, kg }], meta:{ total_kg } }` |
| Kg por temporada (MVP-403) | `GET /api/v1/dashboard/kg-by-season` | — | `200 { data:[{ season_id, season_name, total_kg, harvests }], meta:{ total } }` |
| Kg por terreno (MVP-404) | `GET /api/v1/dashboard/kg-by-plot` | `season_id?`, `plot_ids?[]` | `200 { scope, data:[{ plot_id, plot_name, kg }], meta:{ total_kg } }` |
| Evolución rendimiento (MVP-404) | `GET /api/v1/dashboard/yield-evolution` | `season_id?`, `plot_ids?[]`, `granularity?=month\|week` | `200 { scope, granularity, data:[{ period, yield_l_per_100kg, kg }], history:{ average, average_5_years, average_10_years, prior_years_with_data, window } }` |
| Lectura económica (MVP-707) | `GET /api/v1/dashboard/economics` | `season_id?`, `plot_ids?[]` | `200 { scope, expense, income, harvests, harvests_with_price }` |

> **MVP-707 — Lectura económica (RN-009 ampliada).** `expense` son labores + compras + consumos sin
> compra; las imputaciones quedan fuera porque reparten dinero que la compra ya aportó (`R-01` de
> MVP-399). El servidor **no lo recalcula**: se lo pregunta al diario, que es donde vive esa decisión,
> de modo que panel y diario no pueden discrepar. `income` a `null` significa que **ninguna** partida
> del ámbito tiene precio, y la pantalla debe decir «sin dato», no «0 €». Acotar por `plot_ids` deja
> las compras fuera **por definición** —una compra es del Workspace, no de un terreno—, exactamente
> igual que en el diario.

El dashboard es de **solo lectura** y no se refresca en segundo plano (RN-006): se recalcula al entrar
en la pantalla o a petición explícita. `plot_ids` es un parámetro **repetible**
(`?plot_ids=a&plot_ids=b`).

Reglas de filtro por defecto:

| Regla | Comportamiento |
|---|---|
| Sin `season_id` | backend resuelve la **temporada de trabajo del usuario** que consulta (MVP-209): su `active_season_id` o, en su defecto, `WorkingSeasonPolicy`. Desde MVP-701 el mismo defecto rige en diario, cosechas, compras y consumos (RN-008); el dashboard no admite `all` porque un resumen de campaña sin campaña no significa nada |
| Sin `plot_ids` | backend usa todos los terrenos activos del workspace |
| `scope` en la respuesta (MVP-403) | El ámbito **ya resuelto**: `{ season: { id, name, status, start_date, end_date } \| null, plot_ids[], plots }` (MVP-209: `is_active` → `status` derivado). Los defectos los pone el servidor, así que sin devolverlos la pantalla mostraría cifras sin poder decir de qué son; es también lo que permite posicionar los filtros sin duplicar la regla del defecto en el cliente |
| `season: null` (MVP-403) | El Workspace no tiene temporada que mirar. RN-021 asocia toda la producción a una campaña, así que no es «resumen vacío» sino ámbito imposible: se responden ceros y `null`, y el cliente pide la temporada en vez de presentarlos como datos |
| Terreno pedido inexistente o ajeno (MVP-403) | Se **descarta en silencio**, no es un error: es una lectura, y quien llega con un filtro obsoleto debe ver el dashboard de lo que sí existe. En una escritura la decisión es la contraria (`FOREIGN_KEY_WORKSPACE_MISMATCH`) |
| Terreno **inactivo** pedido explícitamente (MVP-403) | Cuenta. Inactivar deja de ofrecerlo para registros nuevos (MVP-202, CA-3), no borra su histórico: excluir su producción al mirar una campaña pasada falsearía los totales |

Reglas de cálculo (MVP-403):

| Regla | Comportamiento |
|---|---|
| `total_liters` | Litros «cuando exista dato»: declarados o derivados del rendimiento (RN-014). `null` significa **desconocido**, que no es lo mismo que cero litros |
| `average_yield` | Unidad canónica L/100kg (RN-013) y **ponderado por kilos**, no media de partidas: el rendimiento de una campaña es el de todo el aceite sobre toda la aceituna. `null` sin dato de aceite |
| `harvests_with_oil_data` | Partidas que aportan dato de aceite. Junto a `harvests` permite decir sobre cuántas se ha promediado: una media sobre 2 de 20 presentada a secas se lee como la de la campaña entera |
| `data` de kg por destino | Solo los destinos **presentes**. La taxonomía cerrada (RN-012) garantiza que las claves salen del catálogo, no que haya que pintar las cuatro categorías. Orden: kg descendentes y desempate alfabético, el mismo criterio que RN-011 impone al widget de terrenos |
| `meta.total_kg` | Calculado en servidor, para que el porcentaje del gráfico no pueda discrepar del resumen por un redondeo |
| `kg-by-season` | Sin filtro de terreno (la tarjeta del maestro habla de la campaña completa) y en una sola petición. Una campaña sin cosechas aparece con `total_kg: 0`, que es información («no se recolectó nada»), no ausencia de dato. Cierra `P-021` |
| `kg_per_tree` (MVP-405, RN-010) | Kg por árbol del ámbito: Σkg / Σárboles de los terrenos que **han producido y tienen** número de árboles. `null` si ninguno de ellos lo tiene (desconocido, no cero). `trees_counted`/`plots_counted` son el denominador y los terrenos incluidos («sobre X árboles de Y terrenos»); `plots_without_tree_count` son los terrenos con cosechas **excluidos** por no tener `tree_count`, y si es `> 0` la UI avisa de dato incompleto. Un terreno del ámbito sin cosechas no cuenta ni como incluido ni como excluido |
| `kg-by-plot` (MVP-404) | Orden **fijo** por kg descendente y desempate alfabético por nombre de terreno (RN-011). No hay orden manual, así que se resuelve en servidor y el cliente pinta la lista tal cual. Solo los terrenos que **produjeron**: uno sin cosechas sería una barra a cero |
| `yield-evolution` — `data` (MVP-404) | Serie del rendimiento del ámbito por periodo en la unidad canónica L/100kg (RN-013), ponderado por kilos. `period` es `YYYY-MM` (mes) o `YYYY-Www` (semana ISO). Un periodo sin dato de aceite **no aparece**: forzar un cero fingiría una caída que no ocurrió |
| `yield-evolution` — `history` (MVP-404) | Comparativa histórica básica (RN-015): `{ average, average_5_years, average_10_years, prior_years_with_data, window:{ from, to } }`. Es una **ventana de calendario**, no campañas agrupadas: los mismos días de años anteriores a los de las cosechas de la campaña activa —su rango de fechas ensanchado ±7 días para captar más histórico—, buscados en cada año previo. Una cosecha de otra época del año queda fuera. Respeta el filtro de terreno (compara las mismas parcelas). Cada media es `null` mientras no haya «histórico suficiente», medido por profundidad: la general con un año previo con dato; la de 5 años solo si el histórico llega 5 años atrás, la de 10 si llega 10. **Aparece aunque la campaña activa aún no tenga cosechas** (`data` vacío, solo `history`): entonces la ventana la fija el calendario de la temporada. `window` es el tramo (`MM-DD`) usado, para que la UI lo explique |

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

Regla: `yield` y `liters` son opcionales, pero no se permite informar ambos a la vez. `yield` va en la
unidad canónica L/100kg (RN-013).

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
| 400 | `AUTH_GOOGLE_REQUEST_INVALID` | Falta un dato del intercambio con Google (MVP-713) |
| 401 | `AUTH_UNAUTHENTICATED` | Token ausente/inválido |
| 401 | `AUTH_GOOGLE_CODE_INVALID` | El código de autorización de Google ya se usó o caducó (MVP-713) |
| 401 | `AUTH_GOOGLE_TOKEN_INVALID` | El `id_token` de Google no valida |
| 403 | `AUTH_WORKSPACE_FORBIDDEN` | Acceso fuera de workspace |
| 403 | `AUTH_WORKSPACE_SCOPE_REQUIRED` | Operación que exige Workspace activo en la sesión |
| 403 | `AUTH_WORKSPACE_OWNER_REQUIRED` | Operación reservada al propietario del Workspace (baja y traspaso, RN-038) |
| 404 | `RESOURCE_NOT_FOUND` | Recurso inexistente |
| 400 | `VALIDATION_REQUIRED_IF_MATCH` | `PATCH`/`DELETE` de un registro operativo sin `If-Match` (ADR-0005) |
| 400 | `FOREIGN_KEY_WORKSPACE_MISMATCH` | Un vínculo del registro operativo no existe en el Workspace activo |
| 409 | `CONFLICT_VERSION_MISMATCH` | Colisión de versión por edición concurrente |
| 422 | `BUSINESS_RULE_*` | Regla de negocio incumplida |
| 422 | `BUSINESS_RULE_MASTER_IN_USE` | La ficha de maestro tiene histórico; el mensaje dice cuántos registros la referencian (MVP-806) |
| 422 | `BUSINESS_RULE_MASTER_MERGE_SELF` | Se pidió fusionar una ficha consigo misma (MVP-806) |
| 422 | `BUSINESS_RULE_MASTER_MERGE_MEMBER_SURVIVES` | La ficha absorbida es la de un miembro: la suya es siempre la que sobrevive (MVP-806, RN-036) |
| 429 | `RATE_LIMIT_FEEDBACK` | Cupo del canal de sugerencias e incidencias agotado (MVP-711). Lleva `Retry-After` |
| 503 | `FEEDBACK_CHANNEL_UNAVAILABLE` | El canal no tiene buzón o cuenta de envío configurados (MVP-711) |
| 503 | `FEEDBACK_DELIVERY_FAILED` | El proveedor de correo no aceptó el reporte; reintentar tiene sentido (MVP-711) |
| 500 | `AUTH_GOOGLE_EXCHANGE_FAILED` | Fallo propio o del proveedor al intercambiar el código (MVP-713) |
| 500 | `INTERNAL_ERROR` | Error inesperado trazable por `X-Request-Id` |

---

## Política de versionado y breaking changes

Antes de introducir un breaking change, crear ADR y publicar changelog técnico.

1. APIs públicas: deprecación mínima 3 meses.
2. APIs internas: coordinación mínima 1 sprint.
3. Campos nuevos: siempre aditivos cuando sea posible.
4. Eliminación de campos: solo en cambio mayor (`/v2`).
