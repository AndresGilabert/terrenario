---
id: "MVP-699"
tipo: feature
titulo: "TDD: Revision de cierre de la epica MVP-006"
estado: completado
tickets: []
epica: "MVP-006--observabilidad-inicial"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["gobernanza", "calidad", "observabilidad"]
  modulo_path: "03-modulos/"
  componentes: ["ops-signals", "slo", "alerts"]
  etiquetas: ["mvp", "revision-epica", "cierre"]
  nivel_riesgo: medio
creado_en: "2026-08-06"
actualizado_en: "2026-08-06"
---

# TDD: MVP-699 — Correcciones de cierre de la epica MVP-006

> **Referencia al spec**: [spec.md](./spec.md) — los hallazgos `R-01` a `R-05` y el veredicto por
> criterio viven allí. Este documento cubre solo lo que se **cambió**.

## Resumen técnico

Dos correcciones de la propia revisión (`R-01`, `R-03`) más una de higiene (`R-05`). Las tres tocan
código introducido por esta misma épica; `R-02` y `R-04` se derivaron por exceder su alcance.

| Corrección | Qué cambia |
|---|---|
| `R-01` | `GET /api/v1/ops/signals?days=N` devuelve una serie por día |
| `R-03` | La sonda de salud y la ingesta de telemetría salen del divisor del SLO |
| `R-05` | La vigilancia de alertas se apaga en desarrollo |

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
|---|---|---|
| `Application/Ops/OperationalSignalsService.cs` | modificado | Serie diaria, lectura única y publicación de lo excluido |
| `Controllers/OpsController.cs` | modificado | Parámetro `days`, bloque `daily` y `internal_*` |
| `Common/Http/RequestMetricsMiddleware.cs` | modificado | Rutas que no son tráfico de nadie |
| `Infrastructure/Telemetry/TelemetryMetrics.cs` | modificado | Contadores `api.internal.*` |
| `appsettings.Development.json` · `OpsOptions.cs` | modificado | Alertas apagadas en desarrollo |
| `docs/02-arquitectura/contratos-api.md` · `05-infraestructura/observabilidad.md` · `runbooks/revision-operativa.md` | modificado | Serie diaria, exclusión del SLO y cómo comparar semanas |

## Diseño detallado

### `R-01` — La serie diaria sin tocar los SLO

El parámetro `days` gobierna **solo** la serie. Las ventanas de 7 y 30 días de los SLO no se mueven:
son parte de la definición del objetivo, y hacerlas configurables convertiría un acuerdo en una
preferencia de consulta. Hay un test que fija exactamente eso —un desastre de hace diez días entra en
la serie y no en `error_rate_7d`—.

Se emite **un día por fecha del rango, aunque esté vacío**, con recuentos a `0` y cocientes a `null`.
Un hueco en la serie es información: `healthy_minutes = 0` en un día con tráfico esperado dice que la
aplicación no estuvo en pie. Omitir el día lo escondería.

Una sola lectura de la tabla alimenta las tres ventanas (serie, 7 días y 30 días), con el rango más
ancho de los tres.

### `R-03` — Qué cuenta como tráfico del producto

El divisor del SLO tiene que ser aquello por lo que alguien espera. Quedan fuera:

| Ruta | Por qué |
|---|---|
| `/api/v1/health` | Es la sonda del alojamiento, no una persona |
| `/api/v1/ops` | Es el propio equipo consultando |
| `/api/v1/telemetry` · `/api/v1/auth/telemetry` | Es medir, y es fuego y olvido: nadie espera la respuesta |

La comparación es por **segmentos** (`StartsWithSegments`), no por prefijo de texto: `/api/v1/harvests`
no puede caer fuera por parecerse a `/api/v1/health`. Hay un test para eso.

**No se descartan, se apartan**: van a `api.internal.requests` y `api.internal.requests.5xx`, y ambos
se publican en el informe. Un recorte del divisor que no se ve se acaba leyendo como si ese tráfico
nunca hubiera existido —y si la ingesta de telemetría dejara de funcionar, el síntoma sería
precisamente la ausencia de datos, que es lo más difícil de notar—.

### `R-05` — Por qué las alertas se apagan en desarrollo

Una máquina de trabajo con cuenta de envío y destinatario configurados manda correos de alerta reales
por cualquier error transitorio mientras se programa. Ocurrió durante esta revisión. El interruptor ya
existía para el arnés de tests; aquí solo se aplica también a desarrollo.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
|---|---|
| Que `days` moviera también las ventanas de los SLO | Preguntar por más días cambiaría el listón contra el que se compara |
| Omitir de la serie los días sin datos | El hueco es la señal más útil: dice que ese día no se observó nada |
| Descartar sin contar el tráfico de sonda y telemetría | Dejaría de verse si esa ingesta se rompe, que es un fallo silencioso |
| Excluir por prefijo de texto en vez de por segmentos | `/api/v1/harvests` habría quedado fuera por empezar como `health` |
| Corregir `R-02` aquí | La salida depende de qué se decida que es una «sesión activa»: es producto |
| Corregir `R-04` aquí | La corrección vive en autenticación, fuera del alcance de la épica |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| El SLO se queda sin muestras con tráfico muy bajo | media | Es el comportamiento correcto: el volumen mínimo evita juzgar, y `internal_requests_7d` deja ver que el servicio sí estaba sirviendo |
| Alguien añade una ruta interna nueva y vuelve a diluir | media | La lista está en un solo sitio y comentada con la medida que la motivó |
| La serie diaria crece la respuesta | baja | 28 días por defecto, tope de 400, y una sola lectura de tabla |

## Plan de testing

- [x] Serie diaria: rango por defecto, comparación semana contra semana, días vacíos con `0`/`null`,
      acotado del rango pedido, **que el parámetro no mueve las ventanas de los SLO** y que la base se
      lee una sola vez.
- [x] Exclusión del SLO: las cuatro rutas internas no suman a `api.requests` ni al histograma, un 5xx
      interno no contamina la tasa de error, y una ruta de negocio parecida (`/api/v1/harvests`) sigue
      contando.
- [x] Publicación de lo excluido en el informe.
- [x] Integración contra la API real: serie ordenada, sin huecos y con el rango pedido.
- [x] **Verificación end-to-end de la revisión**: rastreo de PII sobre la traza de una sesión
      autenticada (cero coincidencias), rama de error del embudo ejercitada contra la API real, y el
      experimento de dilución antes (sonda = 87 % del divisor) y después (`api.requests = 8`,
      `api.internal.requests = 66`).

## Checklist de implementación

- [x] Diseño técnico revisado y aprobado
- [x] Migraciones de base de datos preparadas (no aplica)
- [x] Tests escritos y pasando
- [x] Documentación de API actualizada
- [x] Módulo afectado actualizado en `docs/03-modulos/` (no aplica)
- [x] Sin `TODO` sin resolver en este documento
