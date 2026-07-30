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
- **La comparativa histórica compara los mismos terrenos en años distintos.** El histórico respeta el
  filtro de terreno, no solo la serie: comparar la campaña actual de dos parcelas contra la media de
  cinco no sería una comparación, sería ruido. Por eso la consulta de evolución pide **todas las
  temporadas pero solo los terrenos del ámbito**, en una sola lectura, y reparte por temporada en
  memoria.
- **«Histórico suficiente» se cuenta sobre temporadas con dato, no sobre el calendario** (RN-015). La
  media general aparece con una temporada previa con rendimiento; las de 5 y 10, solo con al menos 5 y
  10 temporadas previas con dato. Una media «de 5 años» calculada sobre 2 campañas que por casualidad
  tienen dato engañaría más que ayudaría. En este MVP una temporada es la campaña anual, así que «5
  temporadas» ≈ «5 años»; una distinción más fina entre año natural y campaña es post-MVP y no aporta
  nada mientras haya una campaña por año.
- **«Previa» es por fecha de inicio de temporada**, no por el nombre ni el orden de creación: la
  campaña que empezó antes es el histórico de la que empezó después.
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
  average, average_5_seasons, average_10_seasons, prior_seasons_with_data } }`. Cada media del
  histórico es `null` mientras no haya suficiente. `granularity` es `month` (defecto) o `week`.

## Arquitectura de la solución

```text
Controllers/DashboardController.cs          + kg-by-plot y yield-evolution
Application/Dashboard/DashboardQueryService + GetKgByPlotAsync, GetYieldEvolutionAsync,
                                              WeightedYield (extraído), BuildHistory, PeriodKey
Domain/Harvests/IHarvestRepository          HarvestAggregateRow gana `Date`
```

`GetYieldEvolutionAsync` hace **una sola lectura** —todas las temporadas, terrenos del ámbito— y separa
en memoria la serie (temporada actual) del histórico (temporadas previas). Es coherente con la decisión
de `MVP-403`: agregar sobre un único conjunto de filas para que nada se contradiga, con la agregación
detrás del puerto para poder moverla a SQL cuando el volumen lo exija (`ADR-0004`).

## Estrategia de pruebas

`DashboardQueryServiceTests` sube a 28 casos; 11 nuevos:

| Bloque | Qué se fija |
|---|---|
| kg por terreno (CA-1) | Orden por kg descendente (RN-011); desempate alfabético; exclusión de terrenos sin producción; el total cuadra con el resumen |
| Evolución — serie (CA-2) | Agrupación por mes ponderando por kilos; un periodo sin dato de aceite no dibuja punto; agrupación por semana ISO |
| Evolución — histórico (RN-015) | Sin temporadas previas no hay comparativa; con una previa aparece la media general y no las de 5/10; el filtro de terreno viaja también al histórico; sin temporada no se consulta nada |

**Verificación end-to-end conducida**: sembrada una temporada previa (`Campaña 2025`, rendimiento 16) y
cosechas en tres meses de la activa. `kg-by-plot` devuelve La Vía (2260) antes que Matorral (2200,5),
con el total 4460,5 cuadrando con el resumen; `yield-evolution` devuelve los tres meses en orden
(oct 20,33 · nov 19,57 · dic 22,5 —octubre comprobado a mano: 447,391 L sobre 2200,5 kg—) y
`history.average = 16.0` con `prior_seasons_with_data = 1` y las medias de 5/10 en `null`. En UI: los
cuatro widgets en una sola pantalla, las barras de terreno en el orden correcto y el gráfico de
evolución con la línea de referencia histórica y su leyenda «1 temporada anterior». Sin errores de
consola.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Que agregar en memoria no escale con muchas temporadas de histórico | Aislado detrás del puerto; `ADR-0004` prevé la capa analítica y el cambio no toca llamantes |
| Que «5 años» ≠ «5 temporadas» confunda si algún día hay dos campañas por año | Documentado como decisión; el modelo actual tiene una campaña por año, y la distinción es post-MVP |
| Que la semana ISO desconcierte en el cambio de año | El defecto es `month`; `week` es opcional y su clave ordena bien igualmente |
