---
id: "MVP-403"
tipo: feature
titulo: "TDD: Dashboard resumen y kg por destino"
estado: completado
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "kpis"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "resumen", "kg-por-destino"]
  etiquetas: ["mvp", "dashboard", "kpi"]
  nivel_riesgo: medio
creado_en: "2026-07-29"
actualizado_en: "2026-07-29"
---

# TDD: MVP-403 — Dashboard resumen y kg por destino

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Enciende la **Visión General** (`/app/vision-general`), la última entrada que quedaba deshabilitada con
«Pronto» desde `MVP-107`, con los dos primeros widgets del dashboard MVP:

- `GET /api/v1/dashboard/summary` — kilos, litros de aceite y rendimiento medio de la temporada (CA-1).
- `GET /api/v1/dashboard/kg-by-destination` — reparto por destino con la taxonomía cerrada de RN-012,
  incluido `desconocido` (CA-2).
- `GET /api/v1/dashboard/kg-by-season` — **cierra `P-021`**: la producción agregada por campaña que
  enriquece las tarjetas del maestro de temporadas, omitida en `MVP-203` porque `HARVEST` no existía.

Sin migración ni entidad nueva: el dashboard agrega lo que ya hay.

### Decisiones de producto y de diseño tomadas en esta historia

- **Una sola lectura de cosechas por petición, y la agregación en el servidor.** La KB exige que
  resumen, gráficos y detalle **no se contradigan entre sí**; agregando sobre un único conjunto de filas
  eso se cumple *por construcción*, sin depender de que cuatro consultas vean el mismo estado. El puerto
  expone `ListAggregateRowsAsync`, que devuelve solo las columnas que suman —sin `JOIN` a los maestros—,
  y la aritmética vive en `DashboardQueryService`. Cuando el volumen deje de permitirlo, mover los
  `SUM`/`GROUP BY` a SQL (la evolución que ya prevé `ADR-0004` para consultas analíticas) no toca a
  ningún llamante. Es el mismo criterio, y la misma anotación honesta, que `MVP-305` aplicó al diario.
- **El rendimiento medio se pondera por kilos, no por partidas.** El rendimiento de una campaña es el de
  todo el aceite sobre toda la aceituna; una media aritmética daría el mismo peso a un recibo de 50 kg
  que a uno de 5.000. Hay test con un caso donde las dos lecturas divergen (19 frente a 15 L/100kg) para
  que la diferencia no dependa de recordar por qué se hizo así.
- **`total_liters` y `average_yield` pueden ser `null`, y eso no es cero.** «No sabemos cuánto aceite
  salió» y «salieron 0 litros» son afirmaciones distintas, y la segunda sería falsa. La UI muestra «Sin
  dato», no un 0 que parecería un desastre de campaña.
- **Se publica sobre cuántas partidas se ha promediado** (`harvests_with_oil_data` frente a
  `harvests`). Una media calculada sobre 2 de 20 partidas presentada a secas se lee como la de la
  campaña entera; la pantalla lo advierte cuando la cobertura es parcial.
- **El ámbito resuelto viaja en la respuesta** (`scope`). Los defectos de RN-008 los pone el servidor
  —temporada activa y todos los terrenos activos—, así que sin devolverlos la pantalla mostraría cifras
  sin poder decir de qué son. Es también lo que permitirá a `MVP-405` pintar los filtros ya posicionados
  sin duplicar la regla del defecto en el cliente.
- **Un filtro de terreno inexistente o ajeno se descarta en silencio.** Es una **lectura**: quien llega
  con un filtro obsoleto en la URL debe ver el dashboard de lo que sí existe, no una pantalla de error.
  En una escritura la decisión es la contraria (`FOREIGN_KEY_WORKSPACE_MISMATCH`, `MVP-401`), y esa
  asimetría es deliberada.
- **Un terreno inactivo cuenta si se pide explícitamente.** Inactivar deja de ofrecerlo para registros
  nuevos (`MVP-202`, CA-3), no borra su histórico: excluir su producción al mirar una campaña pasada
  falsearía los totales. Lo que hace el defecto de RN-008 es no *ofrecerlo*, no censurarlo.
- **Sin temporada resoluble no se consulta nada.** RN-021 asocia toda la producción a una campaña, así
  que un Workspace sin temporada no tiene un resumen vacío: tiene un ámbito imposible. La respuesta lo
  dice con `season: null` y la pantalla pide la temporada en vez de enseñar ceros.
- **`kg-by-destination` devuelve solo los destinos presentes.** Lo que la taxonomía cerrada garantiza
  (CA-2) es que las claves salen del catálogo de RN-012 y no de texto libre, no que haya que pintar las
  cuatro categorías: las de cero solo añadirían ruido. El total va calculado en servidor para que el
  porcentaje del gráfico no pueda discrepar del resumen por un redondeo.
- **`kg-by-season` va sin filtro de terreno y en una sola petición.** La tarjeta del maestro habla de la
  campaña completa, y una petición por temporada convertiría un maestro de cinco campañas en cinco
  llamadas. Su fallo **no tumba el maestro**: si no se puede calcular, las tarjetas se pintan sin el
  dato, porque la producción es un enriquecimiento y no el maestro.
- **Una campaña sin cosechas aparece con `0 kg`.** Recorrer las temporadas y no los grupos de cosechas
  es lo que permite distinguir «no se recolectó nada» —que es información— de «no hay dato».
- **El botón «Actualizar» es explícito** porque RN-006 prohíbe el refresco continuo. Sin él, la única
  forma de recalcular sería recargar el navegador, y la pantalla no podría explicar que lo que muestra
  es una foto.

### Lo que esta historia deja a `MVP-404` y `MVP-405`

| Pendiente | Quién lo cierra |
|---|---|
| Widgets de kg por terreno y evolución de rendimiento, con la comparativa histórica de RN-015 | `MVP-404` |
| Filtros de temporada y terrenos en la UI, y su persistencia tras recarga (RN-007) | `MVP-405` |
| KPI `kg/árbol` con exclusión de terrenos sin `num_arboles` y aviso de dato incompleto (RN-010) | `MVP-405` |
| Vocabulario de `season_status` en el filtro por temporada (`P-045`) | `MVP-405` |
| Encaje del Home con la Visión General (`P-040`) | `MVP-499` |

El contrato publica `kg_per_tree` e `incomplete` en el resumen; esta historia **no** los emite todavía,
porque emitir un `kg/árbol` sin la exclusión de RN-010 sería publicar una cifra mal calculada. Se añaden
en `MVP-405`, que es donde vive esa regla.

## Contrato

`contratos-api.md` §8 ya contrataba los dos primeros endpoints. Se añade:

- `scope` en las respuestas (ámbito resuelto de RN-008).
- `harvests` y `harvests_with_oil_data` en el resumen.
- `meta.total_kg` en kg por destino.
- `GET /api/v1/dashboard/kg-by-season` (nuevo, cierra `P-021`).

## Arquitectura de la solución

```text
Controllers/DashboardController.cs          borde de transporte (query params y forma de respuesta)
Application/Dashboard/DashboardScope.cs     DashboardRequest · DashboardScope · DashboardScopeResolver
Application/Dashboard/DashboardQueryService resumen · kg por destino · kg por temporada
Domain/Harvests/IHarvestRepository          ListAggregateRowsAsync + HarvestAggregateRow/Filter
```

`HarvestAggregateRow` repite las reglas de rendimiento efectivo de `MVP-402` (`EffectiveYield`) y añade
su simétrica `EffectiveLiters`: el dato no puede cambiar de significado según quién lo lea, así que la
derivación es la misma que la de `HarvestView`.

Frontend: `VisionGeneralView` en una sola pantalla con scroll vertical (RN-005), `dashboard.service.ts`
sobre el cliente HTTP común y `TemporadasView` enriquecida con la producción por campaña. El cliente
HTTP común gana soporte de **query params repetibles** (`?plot_ids=a&plot_ids=b`), que es la forma que
espera la API para los filtros multivalor y que `MVP-405` necesitará para el filtro de terrenos.

## Estrategia de pruebas

`DashboardQueryServiceTests` (17 casos):

| Bloque | Qué se fija |
|---|---|
| Ámbito (RN-008) | Defecto = temporada activa + terrenos activos; un inactivo pedido explícitamente entra; un id inexistente se descarta; sin temporada no se consulta el puerto |
| Resumen (CA-1) | Suma de kilos; litros declarados **y** derivados; `null` sin dato de aceite; ponderación por kilos con un caso donde divergiría de la media aritmética; promedio solo sobre las partidas con dato |
| Destinos (CA-2) | Agrupación y orden por kg descendentes; `desconocido` como categoría propia; no se devuelven categorías a cero; **el total de destinos cuadra con el del resumen** |
| `P-021` | Agregación por campaña; `0 kg` en campañas sin cosechas; sin filtro de terreno |

**Verificación end-to-end conducida**: resumen del Workspace real con `total_kg 3060.5`,
`total_liters 615.65` y `average_yield 20.12` —comprobado a mano: 168,26 + 217,39 + 230 L sobre
3.060,5 kg—; `kg-by-destination` con los tres destinos ordenados y `meta.total_kg` cuadrando con el
resumen; `kg-by-season` devolviendo la campaña con sus 3 partidas; filtro por un terreno bajando a
2.200,5 kg sobre 2 partidas; filtro con un terreno inexistente descartándolo sin error; `season_id`
inexistente devolviendo `season: null` y ceros; Workspace sin temporada devolviendo `season: null`,
`total_liters: null` y `average_yield: null`. En UI: la pantalla con los tres KPIs y el gráfico apilado,
el aviso de «Sin destino», la tarjeta de temporada con «3060,5 kg · 3 partidas» y el estado vacío
«Todavía no hay temporada que mirar» en un Workspace sin campaña.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Que agregar en memoria no escale | Aislado detrás de un único método del puerto; `ADR-0004` ya prevé la capa analítica y el cambio no toca llamantes |
| Que el contrato publique `kg_per_tree` y la API todavía no lo emita | Documentado aquí y en el spec; lo cierra `MVP-405`, que es quien tiene la regla RN-010 |
| Que el bundle del frontend supere los 500 kB (aviso nuevo de Vite) | Sin impacto funcional; se registra para el triage de `MVP-499`, que es donde toca decidir si se parte por rutas |
