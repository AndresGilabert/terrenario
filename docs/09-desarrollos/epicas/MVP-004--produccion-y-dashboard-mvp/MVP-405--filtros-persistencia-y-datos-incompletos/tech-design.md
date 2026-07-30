---
id: "MVP-405"
tipo: feature
titulo: "TDD: Filtros, persistencia y datos incompletos"
estado: completado
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "filtros", "calidad-dato"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "filtros", "kpi-kg-por-arbol"]
  etiquetas: ["mvp", "dashboard", "filters"]
  nivel_riesgo: medio
creado_en: "2026-07-30"
actualizado_en: "2026-07-30"
---

# TDD: MVP-405 — Filtros, persistencia y datos incompletos

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Cierra la experiencia del dashboard MVP con tres piezas:

- **Filtros de temporada y terrenos en la UI** (CA-1). El backend ya los aceptaba desde `MVP-403`
  (`season_id`, `plot_ids[]`) y resolvía los defectos de RN-008; faltaba solo el control de cliente.
- **Persistencia de los filtros en la URL** (CA-2, RN-007). Los filtros viven en los *query params*
  (`?season_id=…&plot_ids=…`), así que la recarga manual los conserva y el enlace es compartible.
  Decisión del PO (2026-07-30) frente a `sessionStorage`/`localStorage`.
- **KPI `kg/árbol` con tratamiento de dato incompleto** (CA-3, RN-010). Nuevo cálculo en servidor sobre
  el resumen: excluye los terrenos **sin `tree_count`** y el widget avisa de que el dato es parcial.

Sin migración ni entidad nueva. El único dato nuevo es el bloque `kg/árbol` en la respuesta de
`GET /dashboard/summary`.

### Decisiones de producto y de diseño

- **Los filtros persisten en la URL, no en almacenamiento** (decisión del PO). Es el patrón estándar de
  un dashboard: la recarga los mantiene (RN-007), el enlace es marcable/compartible y no se «pega» a un
  contexto viejo (p. ej. una temporada que dejó de ser la de trabajo). Se sincronizan con
  `useSearchParams` de react-router, ya presente en la vista.
- **La URL solo lleva lo que el usuario ha elegido.** Sin *query params*, la vista pide sin filtros y el
  servidor aplica los defectos de RN-008 (temporada de trabajo del usuario —MVP-209— y todos los
  terrenos activos); el `scope` que vuelve **posiciona los controles** sin duplicar la regla del defecto
  en el cliente. Al elegir un filtro se escribe en la URL; al volver al defecto se limpia. Así una URL
  limpia siempre significa «lo que el servidor considere por defecto hoy», que es lo que se quiere al
  cambiar de temporada de trabajo.
- **El filtro de terrenos es de selección múltiple** (decisión del PO frente a la única del prototipo).
  El backend ya modela `plot_ids[]` y la URL admite varios ids; permitir un subconjunto da valor real a
  «kg por terreno» y al propio `kg/árbol` de un grupo de parcelas. «Todos» es la ausencia de filtro (no
  se escribe en la URL), coherente con el defecto de RN-008.
- **`kg/árbol` se calcula sobre los terrenos que han producido** y **tienen** número de árboles. Un
  terreno del ámbito sin cosechas no entra (no aporta kilos ni interesa su densidad); de los que sí
  produjeron, los que **no tienen `tree_count`** se **excluyen** del numerador y del denominador y se
  cuentan aparte para el aviso (RN-010). Así el KPI es «kilos por árbol de los árboles que de verdad
  rindieron», y no se diluye con parcelas sin dato ni sin cosecha. El cálculo vive en el **resumen**
  porque es un KPI global de un solo número, junto a kilos, aceite y rendimiento.
- **`null` ≠ 0, también aquí.** Si ningún terreno con cosechas tiene número de árboles, `kg/árbol` es
  `null` («Sin dato»), no cero: la misma regla que el aceite y el rendimiento del resumen.
- **El aviso de dato incompleto es específico del KPI.** El resumen ya tenía un aviso de cobertura
  parcial del **aceite** (partidas sin litros); este es otro eje —terrenos sin árboles— y se muestra por
  separado, contando cuántos terrenos quedaron fuera, para que el usuario sepa que puede completarlos en
  el maestro de Terrenos.
- **`tree_count` no viaja al puerto de agregados.** `HarvestAggregateRow` sigue siendo la fila mínima de
  cosecha; el número de árboles se toma de `scope.Plots`, que el resolutor ya carga en memoria (con
  `TreeCount`). Se evita así un `JOIN` en la lectura agregada, coherente con la decisión de `MVP-403`.

## Contrato

- `GET /dashboard/summary` gana el bloque de `kg/árbol`:
  `{ …, kg_per_tree, trees_counted, plots_counted, plots_without_tree_count }`.
  - `kg_per_tree`: kilos por árbol del ámbito (`null` si ningún terreno con cosechas tiene `tree_count`).
  - `trees_counted` / `plots_counted`: árboles y terrenos sobre los que se ha calculado (para el «sobre
    X árboles de Y terrenos»).
  - `plots_without_tree_count`: terrenos con cosechas **excluidos** por no tener número de árboles;
    `> 0` dispara el aviso de dato incompleto (RN-010).
- El resto de endpoints no cambia: ya aceptaban `season_id` y `plot_ids` y devolvían el `scope`.

## Arquitectura de la solución

```text
Application/Dashboard/DashboardQueryService  SeasonSummary gana kg/árbol; KgPerTree(rows, scope) helper
Controllers/DashboardController              summary expone kg_per_tree y los contadores
types/dashboard.types.ts                     DashboardSummary gana los 4 campos
services/dashboard.service.ts                sin cambios (ya reenvía season_id/plot_ids)
components/dashboard/VisionGeneralView.tsx   filtros (temporada + terrenos) sincronizados con la URL;
                                             KPI kg/árbol (4ª tarjeta) + aviso de dato incompleto
```

El cliente lee las temporadas de `SeasonContext` (`seasons`) y los terrenos activos del
`plot.service`, para poblar los dos controles; las cuatro peticiones del dashboard pasan a llevar los
filtros de la URL. Sin filtros en la URL, el servidor resuelve el defecto y el `scope` de la respuesta
deja los controles posicionados.

## Estrategia de pruebas

| Nivel | Qué cubre |
|---|---|
| `DashboardQueryServiceTests` (CA-3) | `kg/árbol` = Σkg / Σárboles de los terrenos con cosecha y con `tree_count`; exclusión de los que no tienen árboles y su recuento; `null` cuando ninguno tiene árboles; un terreno del ámbito sin cosechas no cuenta; el filtro de terrenos acota el KPI |
| Frontend (conducido) | Los filtros viajan en la URL y la recarga los conserva (CA-2); el defecto se posiciona desde `scope` (CA-1); la tarjeta `kg/árbol` muestra el valor y el aviso de dato incompleto (CA-3) |

**Verificación end-to-end conducida** (dev server + JWT de dev, Workspace «Rafa»):

- **CA-1** — al entrar sin filtros, el ámbito es la temporada de trabajo (Campaña 2026) y los 2 terrenos
  activos; los controles quedan posicionados desde el `scope` y la URL está limpia.
- **CA-2** — elegir «Campana 2025» escribe `?season_id=…`; marcar «La Vía» añade `&plot_ids=…`; **tras
  recargar** la URL y los controles se mantienen (temporada seleccionada, «1 terreno» con La Vía
  marcada) y las cifras corresponden al filtro.
- **CA-3** — con los dos terrenos con árboles, `kg/árbol` = 17,2 sobre 260 árboles de 2 terrenos. Al
  dejar «Matorral» sin `tree_count`, el KPI pasa a 22,6 (solo La Vía, 2.260/100) y aparece el aviso «el
  KPI de kg/árbol excluye 1 terreno con cosechas pero sin número de árboles». Restaurado el dato, vuelve
  a 17,2 y el aviso desaparece. Sin errores de consola.

`GET /dashboard/summary` comprobado vía API: defecto `kg_per_tree=17.16` (4.460,5/260),
`plots_without_tree_count=0`; filtrado a La Vía `kg_per_tree=22.60`, `plots_counted=1` (el filtro acota
el KPI).

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Un `plot_id` obsoleto o ajeno en la URL | El resolutor ya los interseca y descarta en silencio (lectura, no escritura): se ve el dashboard de lo que sí existe |
| Que el KPI se diluya con parcelas sin cosecha o sin árboles | Se calcula solo sobre terrenos con producción y con `tree_count`; los sin árboles se cuentan aparte y disparan el aviso |
| Bucle de escritura URL ↔ estado | La URL es la única fuente de verdad del filtro; el estado deriva de `searchParams`, no al revés |
