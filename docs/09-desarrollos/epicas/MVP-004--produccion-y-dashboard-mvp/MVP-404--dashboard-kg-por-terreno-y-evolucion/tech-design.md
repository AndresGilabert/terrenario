---
id: "MVP-404"
tipo: feature
titulo: "TDD: Dashboard kg por terreno y evolución de rendimiento"
estado: completado
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "kpis", "historico"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "kg-por-terreno", "evolucion-rendimiento"]
  etiquetas: ["mvp", "dashboard", "historico"]
  nivel_riesgo: medio
creado_en: "2026-07-30"
actualizado_en: "2026-07-30"
---

# TDD: MVP-404 — Dashboard kg por terreno y evolución de rendimiento

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Completa los **cuatro widgets** del dashboard MVP (RN-009) con los dos que faltaban:

- `GET /api/v1/dashboard/kg-by-plot` — kilos por terreno en el **orden fijo de RN-011** (kg
  descendentes, desempate alfabético), resuelto en servidor (CA-1).
- `GET /api/v1/dashboard/yield-evolution` — rendimiento del ámbito por mes o semana en la unidad
  canónica L/100kg (RN-013), con la **comparativa histórica básica** de RN-015, presente solo cuando
  hay histórico suficiente (CA-2).

Sin migración ni entidad nueva. La única extensión de datos es un campo `Date` en `HarvestAggregateRow`
(la fila mínima que ya devolvía el puerto en `MVP-403`), para poder agrupar la evolución por periodo.

### Decisiones de producto y de diseño tomadas en esta historia

- **La media ponderada por kilos vive en un solo sitio.** `MVP-403` la calculaba en línea dentro del
  resumen; aquí se extrae a `WeightedYield(rows)` y la usan el resumen, cada terreno (no, ese es kg) y
  **cada periodo y cada ventana histórica** de la evolución. Un ratio solo se promedia bien de una
  forma —litros totales sobre kilos totales—, así que tenerla una vez evita que dos widgets calculen el
  mismo rendimiento de manera distinta.
- **Un periodo sin dato de aceite no es un punto de la serie.** Un mes con cosechas pero sin
  rendimiento no tiene rendimiento que dibujar; forzar un cero fingiría una caída que no ocurrió. Es la
  misma regla que en el resumen (`null` ≠ 0), aplicada al eje temporal.
- **La comparativa histórica es una ventana de calendario, no campañas agrupadas** (decisión del PO,
  2026-07-30). El histórico son «los mismos días de años anteriores» a los de las cosechas de la
  campaña activa: se toma el rango de fechas de esas cosechas —ensanchado **una semana por lado** para
  capturar más histórico— y se busca ese mismo tramo en cada año anterior. Así una parcela se compara
  con lo que ella misma rindió en esas fechas otros años, y una cosecha de otra época (una de primavera
  frente a una campaña de otoño) queda fuera por no ser comparable. La consulta respeta además el filtro
  de terreno —pide todas las temporadas pero solo los terrenos del ámbito, en una sola lectura—, de modo
  que la comparación es de las mismas parcelas.
- **El histórico aparece aunque la campaña activa aún no tenga cosechas** (petición del PO). Es el caso
  de empezar una campaña: no hay línea actual, pero interesa ver a cuánto rindieron esas fechas otros
  años. Sin cosechas, la ventana la fija el **calendario de la propia temporada** (`start_date`..
  `end_date`), que es lo único que dice cuándo se recolecta. La pantalla muestra entonces solo el
  histórico, sin barras ni línea inventada.
- **«Histórico suficiente» se mide por profundidad, no por número de campañas** (RN-015). La media
  general aparece con un año previo con dato en la ventana; la de 5 años, solo si el histórico llega al
  menos 5 años atrás; la de 10, si llega 10. Dentro de esa profundidad, si un tramo no tuvo dato, la
  media queda en `null` de forma natural. Así un «media de 5 años» no se rotula sobre 2 años de datos.
- **La ventana se ancla a las fechas de las cosechas, no a las temporadas ni al calendario natural**:
  «5 años atrás» es cinco años antes del tramo recolectado, sumando años a cada fecha para ver si cae en
  la ventana. Cruzar el fin de año (recolección de diciembre a enero) se resuelve solo, porque se opera
  con fechas completas y no con mes-día sueltos.
- **kg por terreno excluye los terrenos que no produjeron.** Un terreno del ámbito sin cosechas sería
  una barra a cero: ruido en un gráfico que existe para comparar quién aporta más. Es la misma decisión
  que kg por destino tomó en `MVP-403` con las categorías vacías, y es reversible si se decide que ver
  un terreno improductivo es señal.
- **El orden de RN-011 se resuelve en servidor**, no en el cliente: «no hay orden manual» es parte de
  la regla, así que el cliente pinta la lista tal como llega y no puede reordenarla por accidente.
- **`week` usa la semana ISO** (`YYYY-Www`), cuyo año puede diferir del natural en el cambio de año;
  `month` usa `YYYY-MM`. Los dos formatos ordenan cronológicamente como texto, así que la serie no
  necesita ordenar por fecha aparte. El defecto es `month`, que es lo que muestra el prototipo.

### Sobre `MVP-405` (lo que sigue)

Esta historia deja el dashboard con sus cuatro widgets pero **sin filtros en la UI**: el ámbito lo pone
el servidor con los defectos de RN-008. `MVP-405` añade el filtro de temporada y terrenos, su
persistencia tras recarga (RN-007) y el KPI `kg/árbol` con la exclusión de RN-010. El backend ya está
preparado: los cuatro endpoints aceptan `season_id` y `plot_ids` y devuelven el `scope` resuelto, así
que `MVP-405` es sobre todo trabajo de cliente más el `kg/árbol`.

## Contrato

`contratos-api.md` §8 ya listaba los dos endpoints. Se concreta su forma:

- `kg-by-plot`: `{ scope, data:[{ plot_id, plot_name, kg }], meta:{ total_kg } }`. El orden de `data`
  es el de RN-011.
- `yield-evolution`: `{ scope, granularity, data:[{ period, yield_l_per_100kg, kg }], history:{
  average, average_5_years, average_10_years, prior_years_with_data, window:{ from, to } } }`. Cada
  media del histórico es `null` mientras no haya suficiente; `window` es el tramo de calendario (`MM-DD`)
  sobre el que se compara, para que la pantalla lo explique. `granularity` es `month` (defecto) o
  `week`.

## Arquitectura de la solución

```text
Controllers/DashboardController.cs          + kg-by-plot y yield-evolution
Application/Dashboard/DashboardQueryService + GetKgByPlotAsync, GetYieldEvolutionAsync,
                                              WeightedYield (extraído), BuildHistory, PeriodKey
Domain/Harvests/IHarvestRepository          HarvestAggregateRow gana `Date`
```

`GetYieldEvolutionAsync` hace **una sola lectura** —todas las temporadas, terrenos del ámbito— y separa
en memoria la serie (temporada activa) del histórico (ventana de calendario de años anteriores). Es
coherente con la decisión de `MVP-403`: agregar sobre un único conjunto de filas para que nada se
contradiga, con la agregación detrás del puerto para poder moverla a SQL cuando el volumen lo exija
(`ADR-0004`). `BuildHistory` ancla cada cosecha anterior a la ventana con `YearsBack`, que suma años a
su fecha hasta ver si cae dentro; como las ventanas de años consecutivos no se solapan, hay como mucho
un año que la encaja.

## Estrategia de pruebas

`DashboardQueryServiceTests` sube a 33 casos; 16 nuevos:

| Bloque | Qué se fija |
|---|---|
| kg por terreno (CA-1) | Orden por kg descendente (RN-011); desempate alfabético; exclusión de terrenos sin producción; el total cuadra con el resumen |
| Evolución — serie (CA-2) | Agrupación por mes ponderando por kilos; un periodo sin dato de aceite no dibuja punto; agrupación por semana ISO |
| Evolución — histórico (RN-015) | La ventana la fijan las cosechas activas (±7 días); una cosecha de otra época se descarta; el ensanchado de una semana captura las de ±6 días y no las de +10; **solo histórico** cuando la campaña aún no tiene cosechas (ventana = calendario de la campaña); gating de la media de 5 años por la profundidad del histórico; el filtro de terreno viaja también al histórico; sin temporada no se consulta nada |

**Verificación end-to-end conducida** (dos escenarios del PO):

- **Campaña con cosechas.** Sembrada una temporada previa (rendimiento 16) y cosechas en tres meses de
  la activa. `kg-by-plot` devuelve La Vía (2260) antes que Matorral (2200,5), total 4460,5 cuadrando con
  el resumen. `yield-evolution` devuelve los tres meses en orden (oct 20,33 · nov 19,57 · dic 22,5
  —octubre comprobado a mano: 447,391 L sobre 2200,5 kg—), `history.average = 16.0`,
  `prior_years_with_data = 1`, `window = 10-13 … 12-15` (del rango 20-oct…08-dic ±7 días) y las medias
  de 5/10 en `null`. En UI: la línea de barras, el chip «Media histórica: 16,0 L/100kg» y la leyenda
  «los mismos días de años anteriores (13 oct – 15 dic): 1 año anterior con dato».
- **Campaña sin cosechas todavía.** Creada una campaña activa nueva sin recolección. `yield-evolution`
  devuelve `data` vacío (sin línea) y `history.average = 19.97` sobre `prior_years_with_data = 2`, con
  la ventana tomada del calendario de la campaña (`09-01 … 02-28`). La pantalla muestra solo el
  histórico. Sin errores de consola.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Que agregar en memoria no escale con mucho histórico | Aislado detrás del puerto; `ADR-0004` prevé la capa analítica y el cambio no toca llamantes |
| Que una ventana muy estrecha (una sola cosecha) deje fuera histórico cercano | El ensanchado de ±7 días; es un heurístico documentado y ajustable en un único sitio |
| Que la semana ISO desconcierte en el cambio de año | El defecto es `month`; `week` es opcional y su clave ordena bien igualmente |
