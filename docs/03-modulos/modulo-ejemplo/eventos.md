---
modulo: modulo-ejemplo
documento: eventos
actualizado_en: "2026-07-16"
---

# Modulo de ejemplo — Eventos

> Documento de ejemplo. No describe eventos reales del proyecto.

---

## Eventos emitidos por este módulo

| Evento | Cuándo se emite | Consumidores conocidos |
|--------|----------------|----------------------|
| `example.entity.created` | Cuando se crea una nueva entidad | modulo-a |
| `example.entity.updated` | Cuando la entidad cambia de estado | modulo-a, notificaciones |
| `example.entity.failed` | Cuando la operacion falla definitivamente | notificaciones |
| `example.entity.reverted` | Cuando se revierte una operacion | modulo-a, notificaciones |

---

## Estructura de eventos

Todos los eventos siguen el envelope estándar:

```json
{
  "id": "evt_uuid",
  "tipo": "example.entity.updated",
  "version": "1",
  "timestamp": "2025-06-01T10:00:05Z",
  "data": {
    "entity_id": "item_uuid",
    "estado": "completada",
    "referencia_externa": "EXT-12345"
  }
}
```

---

## Eventos consumidos por este módulo

| Evento | Emitido por | Para qué lo usa |
|--------|------------|----------------|
| _(ninguno actualmente)_ | | |

---

## Canal / Broker

> TODO: Describe el broker de mensajería usado (Kafka, RabbitMQ, SQS, etc.)
> y el nombre del topic/queue donde se publican los eventos.

| Evento | Topic / Queue | Broker |
|--------|-------------|--------|
| `example.entity.*` | `example-events` | TODO |
