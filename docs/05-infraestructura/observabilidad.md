---
bloque: 05-infraestructura
documento: observabilidad
actualizado_en: "2026-06-30"
---

# Observabilidad — Monitoring, Alertas y SLOs

---

## Stack de observabilidad

| Herramienta | Propósito |
|------------|-----------|
| TODO (Datadog / Grafana / etc.) | Métricas y dashboards |
| TODO (PagerDuty / OpsGenie) | Gestión de alertas y on-call |
| TODO (Sentry / Rollbar) | Error tracking |
| TODO (ELK / Loki) | Logs centralizados |

---

## SLOs por servicio

### Servicio principal / API

| SLI | SLO | Ventana de medición |
|-----|-----|---------------------|
| Disponibilidad | 99.9% | Rolling 30 días |
| Latencia P95 | < 300ms | Rolling 7 días |
| Tasa de error (5xx) | < 0.1% | Rolling 7 días |

### Embudo de autenticacion (MVP)

| SLI | SLO | Ventana de medición |
|-----|-----|---------------------|
| Conversion login (pantalla -> exito) | >= 85% | Rolling 7 dias |
| Tasa de abandono login | <= 15% | Rolling 7 dias |
| Tiempo medio de login exitoso | <= 45s | Rolling 7 dias |

### Módulo de Payments

| SLI | SLO | Ventana de medición |
|-----|-----|---------------------|
| Disponibilidad | 99.95% | Rolling 30 días |
| Latencia P99 pago | < 2s | Rolling 7 días |

---

## Alertas activas

| Alerta | Condición | Severidad | Canal | Runbook |
|--------|-----------|-----------|-------|---------|
| `HighErrorRate` | Tasa 5xx > 1% durante 5min | 🔴 crítica | PagerDuty | TODO |
| `HighLatency` | P95 > 500ms durante 10min | 🟡 warning | Slack | TODO |
| `ServiceDown` | Health check falla > 1min | 🔴 crítica | PagerDuty | TODO |
| `PaymentFailureSpike` | Tasa de fallo de pagos > 5% | 🔴 crítica | PagerDuty | TODO |
| `LoginAbandonmentSpike` | Abandono login > 25% durante 30min | 🟠 alta | Slack + on-call | TODO |
| `LoginSuccessDrop` | Conversion login < 70% durante 30min | 🟠 alta | Slack + on-call | TODO |

---

## Dashboards

| Dashboard | URL | Audiencia |
|-----------|-----|-----------|
| Overview del sistema | TODO | Todos |
| Payments | TODO | Equipo de pagos |
| Infraestructura | TODO | DevOps / SRE |
| Autenticacion | TODO | Producto + Ingenieria |

## Telemetria del login (obligatoria)

Eventos requeridos:

1. `login_screen_viewed`
2. `login_google_clicked`
3. `login_google_success`
4. `login_google_error`
5. `login_abandonment`

Dimensiones minimas para analisis:

1. `timestamp`
2. `session_id`
3. `flow_id`
4. `channel`
5. `device_type`
6. `error_code` (si aplica)

Reglas de calidad de telemetria:

1. Todo evento de login debe incluir `flow_id` para reconstruir embudo completo.
2. El evento `login_abandonment` se emite por timeout de inactividad o cierre/salida sin exito.
3. Ningun evento puede incluir PII en claro.

---

## Estructura de logs

Todo log de producción debe incluir:

```json
{
  "timestamp": "2025-06-01T10:00:00.000Z",
  "level": "info",
  "service": "payment-service",
  "trace_id": "uuid",
  "span_id": "uuid",
  "message": "Descripción",
  "context": {
    "flow_id": "uuid",
    "event_name": "login_screen_viewed"
  }
}
```

**No loguear nunca**: datos de tarjeta, contraseñas, tokens, PII sin anonimizar.
Ver `../07-seguridad/privacidad-datos.md`.
