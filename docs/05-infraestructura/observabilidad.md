---
bloque: 05-infraestructura
documento: observabilidad
actualizado_en: "2026-07-18"
---

# Observabilidad — Monitoring, Alertas y SLOs

---

## Stack de observabilidad

| Herramienta | Propósito |
|------------|-----------|
| Logs estructurados + request-id | Diagnóstico base de errores y trazabilidad E2E |
| Sentry | Error tracking de aplicación |
| Métricas operativas básicas | Disponibilidad, 5xx y latencia p95 |

Estado fase A: stack liviano y coste controlado.
Escalado a stack completo: al entrar en fase B o antes si hay 2 meses seguidos con alertas críticas recurrentes.

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

---

## Alertas activas

| Alerta | Condición | Severidad | Canal | Runbook |
|--------|-----------|-----------|-------|---------|
| `HighErrorRate` | Tasa 5xx fuera de umbral operativo | critica | canal de incidentes | `runbooks/` |
| `HighLatency` | P95 fuera de umbral operativo | warning | canal de incidentes | `runbooks/` |
| `ServiceDown` | Health check falla > 1min | critica | canal de incidentes | `runbooks/` |
| `LoginAbandonmentSpike` | Abandono login > 25% durante 30min | 🟠 alta | canal privado interno de incidentes | `../08-procesos/gestion-incidentes.md` |
| `LoginSuccessDrop` | Conversion login < 70% durante 30min | 🟠 alta | canal privado interno de incidentes | `../08-procesos/gestion-incidentes.md` |

## Regla de umbrales

1. Baseline inicial de 4 semanas.
2. Revisión mensual de umbrales.

---

## Dashboards

| Dashboard | URL | Audiencia |
|-----------|-----|-----------|
| Overview del sistema | N/A en fase C (revision manual de logs y metricas) | Todos |
| Infraestructura | N/A en fase C (revision manual de logs y metricas) | DevOps / SRE |
| Autenticacion | N/A en fase C (revision manual de embudo login) | Producto + Ingenieria |

## Monitoreo de negocio mínimo (fase A)

Revisión semanal de 15 minutos sobre:

1. `logins_activos_semana`
2. `registros_creados_semana`
3. `tasa_error_funcional_visible`

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
  "service": "terrenario-api",
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

## Trazabilidad KB

1. KPIs y objetivos de uso: `../01-producto/kpis.md`
2. Seguridad y privacidad de logs: `../07-seguridad/modelo-seguridad.md`
3. Arquitectura y evolución post-MVP: `../02-arquitectura/vision-general.md`
