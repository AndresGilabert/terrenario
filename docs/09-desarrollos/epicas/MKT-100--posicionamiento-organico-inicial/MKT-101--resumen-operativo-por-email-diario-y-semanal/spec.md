---
id: "MKT-101"
tipo: feature
titulo: "Resumen operativo por email diario y semanal"
estado: borrador
prioridad: alta
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "2d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: []
bloquea: ["MKT-107", "MKT-110"]
relacionado_con: ["MVP-603", "ADR-0010", "ADR-0011"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["observabilidad", "operacion", "marketing"]
  modulo_path: "03-modulos/observabilidad"
  componentes: ["ops-signals", "smtp", "background-worker"]
  etiquetas: ["email-summary", "daily", "weekly", "ops-alert-email"]
  nivel_riesgo: medio
creado_en: "2026-08-31"
actualizado_en: "2026-08-31"
---

# MKT-101 — Resumen operativo por email diario y semanal

## Contexto

La telemetría ya existe, pero su consulta hoy depende de llamar a `GET /api/v1/ops/signals`. Hace falta
un mecanismo cómodo y periódico para seguir tráfico y conversión sin abrir herramientas adicionales.

## Objetivo

Enviar automáticamente un resumen diario y otro semanal con las métricas clave al mismo destinatario de
alertas operativas (`Ops:AlertEmail`).

## Requisitos de usuario

### HU-1 — Recibir el estado diario sin consulta manual

**Como** responsable del producto,  
**quiero** recibir un correo diario con el resumen del día anterior,  
**para** revisar tráfico y conversión sin hacer llamadas manuales a la API.

### HU-2 — Recibir el estado semanal consolidado

**Como** responsable del producto,  
**quiero** recibir un correo semanal con agregados de 7 días,  
**para** decidir ajustes de contenido y distribución.

## Alcance (in-scope)

- Worker de resumen periódico con dos cadencias: diaria y semanal.
- Métricas mínimas: sesiones, visitas a landing, acceso a login, login exitoso, tasa de conversión y
  principales alertas/SLO de contexto.
- Destinatario único: `Ops:AlertEmail`.
- Reutilización del transporte SMTP existente.

## Fuera de alcance (out-of-scope)

- Nuevas cuentas de correo o nuevos destinatarios.
- UI de configuración en frontend.
- Reportes con segmentaciones avanzadas o adjuntos.

## Criterios de aceptación

- [ ] **CA-1**: El resumen diario se envía una vez al día al `Ops:AlertEmail`.
- [ ] **CA-2**: El resumen semanal se envía una vez cada 7 días al `Ops:AlertEmail`.
- [ ] **CA-3**: Si falla el envío, el proceso se registra y no tumba la API.
