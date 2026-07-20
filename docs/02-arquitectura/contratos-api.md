---
bloque: 02-arquitectura
documento: contratos-api
actualizado_en: "2026-07-20"
---

# Contratos de API

> Contratos iniciales REST para MVP.
> Base funcional y reglas: `../01-producto/definicion-requisitos-usuario.md`, `../01-producto/reglas-de-negocio.md` y decisiones MVP cerradas.

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
- Recursos en plural y kebab-case: `/terrenos`, `/workspace-members`
- Respuestas de error: siempre JSON con `{ "error": { "code": "", "message": "", "details": [] } }`
- Paginación: `?page=1&limit=20` con respuesta `{ "data": [], "meta": { "total": 0, "page": 1, "limit": 20 } }`
- Todas las respuestas incluyen `X-Request-Id` para trazabilidad.
- Concurrencia de escritura: `If-Match` obligatorio en `PATCH`/`DELETE` de entidades críticas.
- El servidor devuelve `409 CONFLICT_VERSION_MISMATCH` cuando la versión enviada no coincide.

### Eventos (mensajería asíncrona)

- Naming de eventos: `{dominio}.{entidad}.{accion}` -> ej: `workspace.miembro.invitado`
- Payload: siempre incluir `id`, `timestamp`, `version`, `data`
- Ver eventos funcionales por módulo en `../03-modulos/{modulo}/eventos.md`

---

## Catálogos cerrados MVP

| Catálogo | Valores permitidos |
|---|---|
| `destino_cosecha` | `venta_aceituna`, `aceite_para_venta`, `aceite_personal`, `desconocido` |
| `producto_cosecha` | catálogo global fijo gobernado por sistema |
| `estado_temporada` | `planificada`, `activa`, `cerrada` |
| `estado_worker_member` | `invitado`, `activo`, `revocado` |

---

## Contratos por flujo MVP

### 1) Terrenos

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta terreno | `POST /api/v1/terrenos` | `nombre*`, `tipo_propiedad*`, `alias?`, `propietario?`, `num_arboles?`, `referencia_catastral?`, `ubicacion?` | `201 { id, workspace_id, ... }` |
| Editar terreno | `PATCH /api/v1/terrenos/{terrenoId}` | campos parciales permitidos | `200 { ...terreno }` |
| Listado terrenos | `GET /api/v1/terrenos` | filtros: `search?`, `activos?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `nombre` obligatorio y longitud válida | `VALIDATION_REQUIRED_NAME` |
| `num_arboles >= 0` | `VALIDATION_RANGE_NUM_ARBOLES` |
| `workspace_id` implícito desde token | `AUTH_WORKSPACE_SCOPE_REQUIRED` |

### 2) Temporadas

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta temporada | `POST /api/v1/temporadas` | `nombre*`, `fecha_inicio*`, `fecha_fin*` | `201 { id, estado: "planificada" }` |
| Editar temporada | `PATCH /api/v1/temporadas/{temporadaId}` | `nombre?`, `fecha_inicio?`, `fecha_fin?`, `estado?` | `200 { ...temporada }` |
| Listado temporadas | `GET /api/v1/temporadas` | `estado?`, `includeClosed?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `fecha_inicio <= fecha_fin` | `VALIDATION_DATE_RANGE_INVALID` |
| No solape exacto de nombre en mismo workspace | `CONFLICT_TEMPORADA_NAME_DUPLICATE` |
| Solo una temporada activa por workspace | `CONFLICT_TEMPORADA_ACTIVE_DUPLICATE` |

### 3) Tareas

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta tarea | `POST /api/v1/tareas` | `nombre*`, `activa?` | `201 { id, nombre, activa }` |
| Editar tarea | `PATCH /api/v1/tareas/{tareaId}` | `nombre?`, `activa?` | `200 { ...tarea }` |
| Listado tareas | `GET /api/v1/tareas` | `activas?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `nombre` obligatorio | `VALIDATION_REQUIRED_TAREA_NOMBRE` |
| tarea pertenece al workspace activo | `AUTH_WORKSPACE_FORBIDDEN` |

### 4) Trabajadores

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta trabajador | `POST /api/v1/trabajadores` | `nombre*`, `activo?` | `201 { id, nombre, activo }` |
| Editar trabajador | `PATCH /api/v1/trabajadores/{trabajadorId}` | `nombre?`, `activo?` | `200 { ...trabajador }` |
| Listado trabajadores | `GET /api/v1/trabajadores` | `activos?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `nombre` obligatorio | `VALIDATION_REQUIRED_NOMBRE` |
| trabajador pertenece al workspace activo | `AUTH_WORKSPACE_FORBIDDEN` |

### 5) Actividades

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta actividad | `POST /api/v1/actividades` | `fecha*`, `terreno_id*`, `temporada_id*`, `trabajador_id*`, `tarea_id?`, `tarea_texto?`, `horas*`, `coste_manual*`, `descripcion?` | `201 { id, ...actividad }` |
| Editar actividad | `PATCH /api/v1/actividades/{actividadId}` | campos parciales | `200 { ...actividad }` |
| Listado actividades | `GET /api/v1/actividades` | `desde?`, `hasta?`, `terreno_id?`, `temporada_id?`, `trabajador_id?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| responsable y horas obligatorios | `VALIDATION_ACTIVITY_REQUIRED_FIELDS` |
| tarea obligatoria por catálogo o texto libre | `VALIDATION_ACTIVITY_TASK_REQUIRED` |
| `horas > 0` | `VALIDATION_ACTIVITY_HOURS_RANGE` |
| `coste_manual >= 0` | `VALIDATION_ACTIVITY_COST_RANGE` |
| Integridad de workspace en FKs | `FOREIGN_KEY_WORKSPACE_MISMATCH` |

### 6) Cosechas

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta cosecha | `POST /api/v1/cosechas` | `fecha*`, `terreno_id*`, `temporada_id*`, `producto*`, `kgs*`, `destino*`, `rendimiento?`, `litros?` | `201 { id, ...cosecha }` |
| Editar cosecha | `PATCH /api/v1/cosechas/{cosechaId}` | campos parciales | `200 { ...cosecha }` |
| Listado cosechas | `GET /api/v1/cosechas` | `desde?`, `hasta?`, `terreno_id?`, `temporada_id?`, `destino?` | `200 { data, meta }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `producto` obligatorio y dentro de catálogo cerrado | `VALIDATION_PRODUCTO_INVALID` |
| `kgs` obligatorio y > 0 | `VALIDATION_COSECHA_KGS_REQUIRED` |
| `rendimiento` y `litros` no pueden coexistir | `VALIDATION_COSECHA_XOR_RENDIMIENTO_LITROS` |
| destino en catálogo cerrado | `VALIDATION_DESTINO_INVALID` |

### 7) Compras

| Operación | Método y ruta | Request (resumen) | Respuesta 2xx |
|---|---|---|---|
| Alta compra | `POST /api/v1/compras` | `fecha*`, `producto*`, `cantidad_total*`, `coste_total*`, `temporada_id*` | `201 { id, precio_unitario, ... }` |
| Editar compra | `PATCH /api/v1/compras/{compraId}` | campos parciales | `200 { ...compra }` |
| Listado compras | `GET /api/v1/compras` | `producto?`, `temporada_id?` | `200 { data, meta }` |
| Imputar compra a terreno | `POST /api/v1/compras/{compraId}/imputaciones` | `terreno_id*`, `cantidad*` | `201 { id, compra_id, terreno_id, cantidad, coste_proporcional }` |

Validaciones clave:

| Regla | Código error |
|---|---|
| `cantidad_total > 0` y `coste_total > 0` | `VALIDATION_COMPRA_TOTALS_RANGE` |
| suma imputaciones <= cantidad total | `VALIDATION_IMPUTACION_OVERFLOW` |

### 8) Dashboard

| Operación | Método y ruta | Request (query) | Respuesta 2xx |
|---|---|---|---|
| Resumen temporada | `GET /api/v1/dashboard/resumen` | `temporada_id?`, `terrenos_ids?[]` | `200 { kg_total, litros_total, rendimiento_medio, kg_por_arbol, incompleto }` |
| Kg por destino | `GET /api/v1/dashboard/kg-por-destino` | `temporada_id?`, `terrenos_ids?[]` | `200 { data:[{ destino, kg }] }` |
| Kg por terreno | `GET /api/v1/dashboard/kg-por-terreno` | `temporada_id?` | `200 { data:[{ terreno_id, terreno_nombre, kg }] }` |
| Evolución rendimiento | `GET /api/v1/dashboard/evolucion-rendimiento` | `temporada_id?`, `granularidad?=mes\|semana` | `200 { data:[{ periodo, rendimiento }] }` |

Reglas de filtro por defecto:

| Regla | Comportamiento |
|---|---|
| Sin `temporada_id` | backend resuelve temporada activa del workspace |
| Sin `terrenos_ids` | backend usa todos los terrenos activos del workspace |

### 9) Alcance de sincronización MVP

MVP v1 opera en modo online. No se incluyen endpoints de sincronización diferida u outbox en `v1`.

Los endpoints de sincronización se definirán en una versión posterior cuando se active el alcance offline.

---

## Esquemas JSON mínimos (extracto)

### `CosechaCreateRequest`

```json
{
  "fecha": "2026-10-05",
  "terreno_id": "uuid",
  "temporada_id": "uuid",
  "producto": "aceituna_olivar",
  "kgs": 1200.5,
  "destino": "aceite_para_venta",
  "rendimiento": 18.5,
  "litros": null
}
```

Regla: `rendimiento` y `litros` son opcionales, pero no se permite informar ambos a la vez.

### `ActividadCreateRequest`

```json
{
  "fecha": "2026-09-20",
  "terreno_id": "uuid",
  "temporada_id": "uuid",
  "trabajador_id": "uuid",
  "tarea_texto": "Poda de mantenimiento",
  "horas": 4.5,
  "coste_manual": 70.0,
  "descripcion": "Poda de mantenimiento"
}
```

---

## Errores estándar

| HTTP | Código | Uso |
|---|---|---|
| 400 | `VALIDATION_*` | Error de campos o formato |
| 401 | `AUTH_UNAUTHENTICATED` | Token ausente/inválido |
| 403 | `AUTH_WORKSPACE_FORBIDDEN` | Acceso fuera de workspace |
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
