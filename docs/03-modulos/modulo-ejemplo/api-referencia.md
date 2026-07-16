---
modulo: modulo-ejemplo
documento: api-referencia
actualizado_en: "2026-07-16"
---

# Modulo de ejemplo — Referencia de API

> Documento de ejemplo. No describe una API real del proyecto.
> Versión actual: `v1`
> Base URL: `/api/v1/example-module`
> Autenticación: Bearer JWT

---

## Endpoints

### POST /api/v1/example-module/items

Crea e inicia el procesamiento de una nueva entidad del modulo.

#### Request

```json
{
  "campo_principal": "valor",
  "categoria": "tipo-a",
  "metadata": {
    "origen": "interno"
  },
  "referencia_externa": "EXT-12345"
}
```

#### Response 201

```json
{
  "data": {
    "id": "item_uuid",
    "estado": "pendiente",
    "campo_principal": "valor",
    "creado_en": "2025-06-01T10:00:00Z"
  }
}
```

#### Errores

| Código | Error | Descripción |
|--------|-------|-------------|
| 400 | `validation_error` | Campos obligatorios ausentes o inválidos |
| 409 | `invalid_state` | Estado actual no permite esta operacion |
| 422 | `invalid_method` | Metodo no soportado |

---

### GET /api/v1/example-module/items/{id}

Obtiene el estado de una entidad.

#### Response 200

```json
{
  "data": {
    "id": "item_uuid",
    "estado": "completada",
    "campo_principal": "valor",
    "creado_en": "2025-06-01T10:00:00Z",
    "actualizado_en": "2025-06-01T10:00:05Z"
  }
}
```

---

### POST /api/v1/example-module/items/{id}/actions

Ejecuta una accion sobre una entidad existente.

#### Request

```json
{
  "accion": "revertir",
  "motivo": "solicitud_operativa"
}
```

#### Response 201

Estructura igual a GET con un estado actualizado.

#### Errores

| Código | Error | Descripción |
|--------|-------|-------------|
| 404 | `not_found` | Entidad no encontrada |
| 409 | `invalid_state` | El estado actual no permite la accion |
| 422 | `invalid_payload` | El payload de la accion no es valido |

---

## Webhooks entrantes

El modulo expone `POST /api/v1/example-module/webhook` para recibir notificaciones externas.
La validacion de firma se realiza con `EXAMPLE_MODULE_WEBHOOK_SECRET`.
