---
bloque: 02-arquitectura
documento: contratos-api
actualizado_en: "2026-07-24"
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
| `harvest_destination` | `venta_aceituna`, `aceite_para_venta`, `aceite_personal`, `desconocido` |
| `harvest_product` | catálogo global fijo gobernado por sistema |
| `season_status` | `planificada`, `activa`, `cerrada` |
| `worker_member_status` | `invitado`, `activo`, `revocado` |
| `invitation_channel` | `email`, `enlace` |
| `invitation_status` | `pendiente`, `aceptada` |

---

## Contratos por flujo MVP

### 0) Workspaces

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta workspace | `POST /api/v1/workspaces` | `name*` | `201 { workspace: { id, name }, access_token, expires_in }` |
| Mis workspaces | `GET /api/v1/workspaces` | — | `200 { data:[{ id, name, role, status, is_active, joined_at }], meta:{ total, active_workspace_id } }` |
| Workspace activo | `GET /api/v1/workspaces/active` | — | `200 { id, name }` |
| Cambiar workspace activo | `PUT /api/v1/workspaces/active` | `workspace_id*` | `200 { workspace: { id, name }, access_token, expires_in }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `name` obligatorio | `VALIDATION_REQUIRED_WORKSPACE_NAME` |
| `name` de longitud válida | `VALIDATION_WORKSPACE_NAME_LENGTH` |
| El usuario todavía no tiene ningún Workspace | `WORKSPACE_NOT_FOUND` (404) |
| `workspace_id` obligatorio al cambiar de activo | `VALIDATION_REQUIRED` (400) |
| Activar un Workspace sin membresía activa | `AUTH_WORKSPACE_FORBIDDEN` (403) |

Reglas de contexto:

| Regla | Comportamiento |
|---|---|
| El creador queda como miembro activo del Workspace | Membresía `workspace_owner` creada en la misma transacción |
| El Workspace activo viaja en el claim `workspace_id` del `access_token` | Nunca se acepta como parámetro del cliente |
| `POST /workspaces` reemite la sesión | Devuelve un `access_token` nuevo ya situado en el Workspace creado |
| `PUT /workspaces/active` reemite la sesión | Valida membresía activa, persiste el activo (`users.active_workspace_id`) y devuelve un `access_token` nuevo situado en el destino (MVP-104) |
| `GET /workspaces` solo lista membresías `activo` | Las `revocado` quedan fuera; `is_active` marca el que ejecuta las operaciones |

### 0.b) Invitaciones a Workspace

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Emitir invitación | `POST /api/v1/workspaces/invitations` | `channel*`, `email?` | `201 { id, channel, email, status, accept_url, expires_at, email_sent }` |
| Invitaciones pendientes (emitidas) | `GET /api/v1/workspaces/invitations` | — | `200 { data, meta: { total } }` |
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
| `viewer.can_accept` / `viewer.reason` (MVP-107) | Aptitud de la cuenta autenticada calculada antes de aceptar; `reason` ∈ `email_mismatch`, `expired`, `already_used`, `already_rejected`, `already_member`. No revela el email destinatario |
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
| `name` obligatorio y longitud válida | `VALIDATION_REQUIRED_NAME` |
| `tree_count >= 0` | `VALIDATION_RANGE_TREE_COUNT` |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` |

### 2) Seasons (temporadas)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta temporada | `POST /api/v1/seasons` | `name*`, `start_date*`, `end_date*` | `201 { id, status: "planificada" }` |
| Editar temporada | `PATCH /api/v1/seasons/{seasonId}` | `name?`, `start_date?`, `end_date?`, `status?` | `200 { ...season }` |
| Listado temporadas | `GET /api/v1/seasons` | `status?`, `include_closed?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `start_date <= end_date` | `VALIDATION_DATE_RANGE_INVALID` |
| No solape exacto de nombre en mismo workspace | `CONFLICT_SEASON_NAME_DUPLICATE` |
| Solo una temporada activa por workspace | `CONFLICT_SEASON_ACTIVE_DUPLICATE` |

### 3) Tasks (tareas)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta tarea | `POST /api/v1/tasks` | `name*`, `is_active?` | `201 { id, name, is_active }` |
| Editar tarea | `PATCH /api/v1/tasks/{taskId}` | `name?`, `is_active?` | `200 { ...task }` |
| Listado tareas | `GET /api/v1/tasks` | `is_active?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `name` obligatorio | `VALIDATION_REQUIRED_TASK_NAME` |
| tarea pertenece al workspace activo | `AUTH_WORKSPACE_FORBIDDEN` |

### 4) Workers (trabajadores)

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta trabajador | `POST /api/v1/workers` | `name*`, `is_active?` | `201 { id, name, is_active }` |
| Editar trabajador | `PATCH /api/v1/workers/{workerId}` | `name?`, `is_active?` | `200 { ...worker }` |
| Listado trabajadores | `GET /api/v1/workers` | `is_active?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `name` obligatorio | `VALIDATION_REQUIRED_NAME` |
| trabajador pertenece al workspace activo | `AUTH_WORKSPACE_FORBIDDEN` |

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
