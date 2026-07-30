---
id: "MVP-405"
tipo: feature
titulo: "Filtros, persistencia y datos incompletos"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-403", "MVP-404"]
bloquea: ["MVP-005", "MVP-006"]
relacionado_con: ["MVP-209"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "filtros", "calidad-dato"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "filtros", "kpi-kg-por-arbol"]
  etiquetas: ["mvp", "dashboard", "filters"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-30"
---

# MVP-405 — Filtros, persistencia y datos incompletos

## Contexto

El dashboard MVP no solo necesita widgets: también debe respetar contexto de Workspace, temporada y terrenos, conservar filtros tras recarga manual y tratar explícitamente los casos en que faltan datos base como `num_arboles`.

## Objetivo

Cerrar la experiencia operativa del dashboard con filtros coherentes, persistencia mínima y tratamiento explícito del dato incompleto.

## Requisitos de usuario

### HU-1 — Mantener el contexto de lectura del dashboard

**Como** usuario que revisa la temporada,
**quiero** filtrar por temporada y terrenos sin perder ese contexto al recargar,
**para** consultar resultados de forma rápida y consistente.

### HU-2 — Entender cuándo un KPI está incompleto

**Como** usuario del Workspace,
**quiero** que la app me avise cuando falten datos base,
**para** no interpretar como exacto un KPI calculado sobre información parcial.

## Alcance (in-scope)

- Filtro por temporada y terrenos en dashboard.
- Resolución por defecto de temporada activa y todos los terrenos activos.
- Persistencia de filtros tras recarga manual.
- Tratamiento de `kg/árbol` con exclusión de terrenos sin `num_arboles` y aviso de dato incompleto.
- Dashboard en una sola pantalla con scroll vertical y sin refresco continuo.

## Fuera de alcance (out-of-scope)

- Filtros avanzados por propietario o dimensiones adicionales fuera del núcleo MVP.
- Refresco en tiempo real.
- Configuración de paneles o widgets personalizados.

## Criterios de aceptación

- [x] **CA-1**: El dashboard aplica por defecto la **temporada de trabajo del usuario** (MVP-209) y todos los terrenos activos cuando no se informan filtros. _(Verificado: sin `query params`, ámbito = Campaña 2026 + 2 terrenos activos, controles posicionados desde `scope`.)_
- [x] **CA-2**: La recarga manual conserva los filtros activos del usuario. _(Verificado: los filtros viven en la URL —`?season_id=…&plot_ids=…`—; recargar mantiene URL, controles y cifras. Decisión del PO: persistencia en URL frente a storage.)_
- [x] **CA-3**: El KPI `kg/árbol` excluye terrenos sin `num_arboles` e informa explícitamente que el dato es incompleto. _(Verificado: con «Matorral» sin árboles, el KPI se calcula solo sobre «La Vía» y aparece el aviso de exclusión; restaurado el dato, vuelve al valor completo.)_

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/TerrenosView.tsx](../../../../../prototype/terrenario-mvp/src/components/TerrenosView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| DashboardView - filtros | RN-007, RN-008 | cubierto | Filtros de temporada y terrenos en la URL; recarga conducida conserva URL, controles y cifras |
| TerrenosView + DashboardView | RN-010 | cubierto | KPI `kg/árbol` excluye terrenos sin `tree_count` y avisa; verificado anulando y restaurando el dato de un terreno |

## Notas y decisiones

- Esta historia cierra la experiencia completa del dashboard MVP antes del endurecimiento y la observabilidad.
- **Persistencia de filtros en la URL** (decisión del PO, 2026-07-30) frente a `sessionStorage`/`localStorage`: recarga conservada (RN-007), enlace compartible y sin «pegarse» a un contexto viejo. Los defectos siguen siendo del servidor (RN-008): sin `query params`, la URL limpia significa «lo que el servidor considere por defecto hoy».
- **Filtro de terrenos de selección múltiple** (decisión del PO) frente a la única del prototipo: el backend ya modelaba `plot_ids[]` y da valor a comparar un subconjunto de parcelas.
- **El defecto de temporada es la de trabajo del usuario** (MVP-209), no «la activa del Workspace»: el rediseño previo dejó el `DashboardScopeResolver` ya resolviendo por usuario.
- **`kg/árbol` se calcula sobre los terrenos con cosecha y con número de árboles**; los que produjeron sin `tree_count` se excluyen y disparan el aviso (RN-010). El backend ya aceptaba los filtros desde `MVP-403`, así que esta historia fue sobre todo cliente más este KPI.
