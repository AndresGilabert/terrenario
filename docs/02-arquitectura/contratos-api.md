---
bloque: 02-arquitectura
documento: contratos-api
actualizado_en: ""
---

# Contratos de API

> Contratos de comunicación entre servicios internos y con clientes externos.
> Las especificaciones OpenAPI completas viven junto al código de cada servicio.

---

## APIs públicas (expuestas a clientes)

| API | Versión | Especificación | Autenticación |
|-----|---------|---------------|--------------|
| TODO | v1 | `link/a/openapi.yaml` | JWT / API Key |

---

## APIs internas (entre servicios)

| Servicio origen | Servicio destino | Protocolo | Descripción |
|----------------|-----------------|-----------|-------------|
| TODO | TODO | REST / gRPC / Eventos | |

---

## Convenciones de API

### REST

- Versionado en la URL: `/api/v1/...`
- Recursos en plural y kebab-case: `/payment-transactions`
- Respuestas de error: siempre JSON con `{ "error": { "code": "", "message": "" } }`
- Paginación: `?page=1&limit=20` con respuesta `{ "data": [], "meta": { "total": 0 } }`

### Eventos (mensajería asíncrona)

- Naming de eventos: `{dominio}.{entidad}.{acción}` → ej: `payments.transaction.created`
- Payload: siempre incluir `id`, `timestamp`, `version`, `data`
- Ver eventos por módulo en `../03-modulos/{modulo}/eventos.md`

---

## Políticas de breaking changes

> Antes de introducir un breaking change, crear un ADR en `decisiones/`.

- Las APIs públicas requieren un período de deprecación de **mínimo 3 meses**
- Notificar a los consumidores internos con **mínimo 1 sprint** de antelación
- Las APIs internas pueden cambiar con **1 sprint** de coordinación entre equipos
