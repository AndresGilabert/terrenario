---
id: "RB-001"
titulo: "Revision operativa y respuesta a las alertas del MVP"
servicio: "terrenario-api"
owner: "@andres"
ultima_revision: "2026-08-06"
tiempo_estimado: "15 minutos (revision) / variable (incidente)"
---

# Runbook: Revision operativa y respuesta a las alertas del MVP

> **Servicio**: terrenario-api
> **Tiempo estimado**: 15 minutos la revision semanal
> **Owner**: @andres

---

## Cuándo usar este runbook

Dos situaciones distintas:

1. **Revision semanal** de 15 minutos que exige `../observabilidad.md` ("monitoreo de negocio minimo").
2. **Ha llegado un aviso de alerta** por correo o aparece en la traza como `alert.fired`.

**Alertas cubiertas**: `ServiceDown`, `HighErrorRate`, `HighLatency`, `LoginAbandonmentSpike`,
`LoginSuccessDrop`.

---

## Diagnóstico previo

Antes de nada, una sola peticion contesta a casi todo:

- [ ] Tener a mano la llave de operacion (`Ops__ApiKey`, en el gestor de secretos).
- [ ] Comprobar que la aplicacion responde.

```bash
curl -s https://app.terrenario.com/api/v1/health
```

**Resultado esperado**: `{"status":"healthy","database":"healthy"}` con codigo `200`.
Un `503` con `"database":"unreachable"` significa que el proceso vive pero no alcanza la base de
datos: ir directamente al paso de `ServiceDown`.

```bash
curl -s -H "X-Ops-Key: $OPS_API_KEY" https://app.terrenario.com/api/v1/ops/signals
```

**Resultado esperado**: el informe completo. Un `404` significa que **no hay llave configurada** en el
despliegue, no que la ruta no exista.

---

## Revision semanal (15 minutos)

Del informe anterior, mirar cinco cosas y anotarlas:

| Que mirar | Campo | Objetivo (KB) |
|---|---|---|
| Conversion de login | `login_funnel_7d.conversion` | >= 0.85 |
| Abandono de login | `login_funnel_7d.abandonment_rate` | <= 0.15 |
| Uso del dashboard | `product_usage_7d.dashboard_usage` | >= 0.85 |
| Cobertura de widgets | `product_usage_7d.widget_coverage` | 1.0 |
| Negocio minimo | `business_7d.logins`, `.records_created`, `.visible_error_rate` | Sin objetivo: tendencia |

Y los dos SLO tecnicos: `slo.error_rate_7d` (< 0.001) y `slo.latency_p95_7d_ms` (< 300).

Un valor a `null` **no es un cero**: significa que no hubo nada sobre lo que calcular esa semana. No
apuntarlo como caida.

Los objetivos siguen pendientes de baseline (`../../01-producto/kpis.md`): las primeras cuatro semanas
sirven para fijarlo, no para comparar.

---

## Pasos de resolución por alerta

### `ServiceDown` — la comprobacion de salud falla

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://app.terrenario.com/api/v1/health
az webapp log tail --resource-group rg-terrenario-prod --name app-terrenario-api
```

1. Si responde `503` con `"database":"unreachable"`: el problema es PostgreSQL, no la aplicacion.
   Comprobar el estado del servidor flexible en Azure y sus reglas de red.
2. Si no responde nada: el proceso esta caido. Revisar el log de arranque; la causa mas probable es
   una **migracion fallida**, que impide arrancar a proposito (ver `../../02-arquitectura/`).
3. Tras recuperar, la alerta se cierra sola y emite `alert.resolved` con lo que duro.

**Resultado esperado**: `200` y `{"status":"healthy","database":"healthy"}`.

---

### `HighErrorRate` — mas de un 1 % de 5xx

```bash
az webapp log tail --resource-group rg-terrenario-prod --name app-terrenario-api \
  | grep -i "error"
```

1. Localizar el `X-Request-Id` de las respuestas con fallo y buscarlo en la traza: correlaciona la
   peticion con su error (`P-006`).
2. Si los 5xx se concentran en una ruta, es un fallo de esa funcionalidad; si estan repartidos, mirar
   primero base de datos y memoria.
3. Si la causa es una publicacion reciente, la vuelta atras es **publicar el tag anterior**
   (`../../08-procesos/proceso-release.md`).

**Resultado esperado**: `live.error_rate` por debajo de `0.01` en la siguiente consulta.

---

### `HighLatency` — P95 por encima de 500 ms

1. Comprobar si coincide con un pico de trafico (`live.requests`) o es constante.
2. Si es constante y no hay trafico, mirar la base de datos: el plan B1ms es pequeno y una consulta
   sin indice se nota entera.
3. Es un **aviso**, no una critica: no exige actuar de madrugada.

**Resultado esperado**: `live.latency_p95_ms` por debajo de 500.

---

### `LoginAbandonmentSpike` y `LoginSuccessDrop` — el embudo de acceso

Las dos suelen saltar juntas, porque miden las dos caras de lo mismo.

1. Comprobar primero que **no es un fallo tecnico**: mirar `login_funnel_7d.errors` y buscar
   `login_google_error` en la traza. Un problema con Google OIDC (credenciales caducadas, dominio de
   redireccion mal configurado) se ve ahi.
2. Si no hay errores, el acceso funciona y la gente **se esta yendo**: es un problema de producto, no
   de operacion. Anotarlo para la revision y no tratarlo como incidente.
3. Ojo al volumen: con pocas visitas, un par de abandonos mueven mucho el porcentaje. La alerta ya
   exige 10 pantallas en la ventana, pero 10 sigue siendo poco para concluir nada.

**Resultado esperado**: entender cual de los dos casos es. No siempre hay nada que arreglar.

---

## Verificación

```bash
curl -s -H "X-Ops-Key: $OPS_API_KEY" https://app.terrenario.com/api/v1/ops/signals \
  | grep -o '"state":"firing"' | wc -l
```

**Resultado esperado tras la resolución**: `0`. Ademas, cada alerta resuelta deja un
`alert.resolved` en la traza con cuanto duro.

---

## Escalación

1. Escala a: @andres (responsable tecnico y de producto).
2. Canal de comunicacion: el correo configurado en `Ops__AlertEmail`.
3. Informacion a proporcionar: la respuesta completa de `/api/v1/ops/signals`, el `X-Request-Id` de un
   fallo concreto si lo hay, y la hora de inicio (`firing_since`).

---

## Postmortem

Si el incidente fue severo, sigue el proceso en `../../08-procesos/gestion-incidentes.md`.
