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

### Como se explota (MVP-601)

Cada evento sale por **dos caminos** y ninguno sustituye al otro:

1. **Log estructurado** (`auth.funnel`), con las seis dimensiones. Sirve para mirar un caso concreto
   mientras el log siga a mano. Fuera de desarrollo el log se emite en **JSON con `timestamp`**: con el
   formateador de texto, las dimensiones salen interpoladas dentro de una frase y reconstruir el embudo
   pasaria por analizar prosa.
2. **Contadores diarios agregados**, en la tabla `telemetry_daily_counters` de la propia base de datos.
   Es lo que hace calculables las ventanas de 7 y 30 dias que piden los SLO: los logs de App Service no
   se retienen de forma fiable, asi que sobre ellos esas ventanas no existen.

Por que **contadores** y no una traza de eventos persistida: un contador responde a todos los KPI de la
KB y **no conserva ningun identificador**, asi que no anade una categoria de dato personal a `RN-041`
ni al inventario de `RN-042`. La traza individual habria permitido analisis no previstos, que es
justamente lo que la KB deja fuera de alcance en esta epica.

Contadores del embudo:

| Contador | Que cuenta |
|---|---|
| `login.screen_viewed` | Entradas a la pantalla de login |
| `login.google_clicked` | Clics en «Continuar con Google» |
| `login.success` | Accesos completados |
| `login.error` y `login.error.{codigo}` | Fallos del intercambio, en total y por codigo |
| `login.abandonment` | Abandonos (por inactividad o por salida sin exito) |
| `login.success.duration_ms.sum` · `login.success.timed` | Suma de duraciones y su divisor, para el «tiempo medio de login exitoso» |

Se guarda **suma y divisor**, no la media: las medias no se agregan entre dias, asi que la de la semana
no es la media de las medias diarias. El divisor es `login.success.timed` y no `login.success` porque un
reinicio deja exitos cuyo instante de inicio se desconoce; contarlos con duracion cero rebajaria la
media y haria creer que el acceso es mas rapido de lo que es.

Con esto, los KPI de `../01-producto/kpis.md` salen de una consulta:

- Conversion de login = `login.success` / `login.screen_viewed`
- Tasa de abandono = `login.abandonment` / `login.screen_viewed`
- Tiempo medio de login exitoso = `login.success.duration_ms.sum` / `login.success.timed`

Retencion: los contadores se conservan 400 dias (`Telemetry:RetentionDays`) y se podan a diario. No es
un plazo de `RN-041` —no hay datos personales que expurgar—, sino higiene de tabla.

### Uso del producto (MVP-602)

Mismo mecanismo —log estructurado (`product.usage`) mas contador diario— para las senales de uso.

| Contador | Que cuenta |
|---|---|
| `app.session_started` | Sesiones que llegan al area autenticada. **Es el divisor** del uso del dashboard |
| `dashboard.viewed` | Entradas al dashboard, todas |
| `dashboard.session_with_view` | Sesiones que abren el dashboard **al menos una vez** |
| `dashboard.manual_refresh` | Pulsaciones de «Actualizar» (RN-006) |
| `dashboard.widget.rendered` · `dashboard.widget.blocked` | Widgets que se pudieron mostrar y los que no |
| `dashboard.widget.{widget}.{ok\|empty\|error}` | Desglose, para saber **cual** falla y no solo que algo falla |

KPI de producto de `../01-producto/kpis.md`:

- Uso del dashboard en sesiones activas = `dashboard.session_with_view` / `app.session_started`
- Recargas manuales por sesion = `dashboard.manual_refresh` / `dashboard.session_with_view`
- Cobertura de widgets MVP = `dashboard.widget.rendered` / (`rendered` + `blocked`)

Tres matices que cambian lo que significan estas cifras:

1. **Sesiones, no visitas.** `dashboard.session_with_view` existe porque el KPI pregunta por sesiones:
   quien entra ocho veces en una sesion sigue siendo una sesion, y contar visitas daria porcentajes por
   encima del 100 %.
2. **La sesion activa se cuenta al entrar a la aplicacion**, no al abrir el dashboard. Contarla en el
   propio dashboard haria que el porcentaje fuese siempre 100 %.
3. **`empty` no es `error`.** El KPI admite expresamente los estados vacio/incompleto: un Workspace que
   aun no ha cosechado no tiene el dashboard roto. Solo `error` resta cobertura.

Limite conocido: la senal de widget bloqueado viaja **por la propia API**, asi que cubre el fallo de un
widget concreto, no una caida total del servicio —en ese caso tampoco llegaria la senal—. La
disponibilidad se mide aparte (`MVP-603`).

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
