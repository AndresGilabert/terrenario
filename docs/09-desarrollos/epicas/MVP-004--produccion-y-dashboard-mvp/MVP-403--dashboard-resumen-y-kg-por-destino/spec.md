---
id: "MVP-403"
tipo: feature
titulo: "Dashboard resumen y kg por destino"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-401", "MVP-402"]
bloquea: ["MVP-405"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "kpis"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "resumen", "kg-por-destino"]
  etiquetas: ["mvp", "dashboard", "kpi"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-29"
---

# MVP-403 — Dashboard resumen y kg por destino

## Contexto

El dashboard MVP debe empezar por el bloque de lectura más directo: resumen de temporada y reparto de kilos por destino. Estos widgets convierten la producción ya registrada en una vista rápida y comparable por Workspace.

## Objetivo

Mostrar un resumen de temporada útil y un desglose fiable por destino a partir de las cosechas registradas en el MVP.

## Requisitos de usuario

### HU-1 — Ver un resumen claro de temporada

**Como** usuario que revisa resultados,
**quiero** ver los principales indicadores de la temporada,
**para** entender de un vistazo el estado productivo del Workspace.

### HU-2 — Ver el reparto por destino

**Como** usuario del Workspace,
**quiero** consultar los kg agrupados por destino,
**para** entender cómo se está distribuyendo la producción registrada.

## Alcance (in-scope)

- Widget de resumen de temporada.
- Cálculo de `kg_total`, `litros_total` cuando exista dato y `rendimiento_medio`.
- Widget de kg por destino respetando taxonomía cerrada.
- Inclusión de categoría `desconocido` con alias visual permitido.

## Fuera de alcance (out-of-scope)

- Comparativas analíticas avanzadas por múltiples dimensiones.
- Cuadros de mando financieros.
- Drill-down complejo fuera de los filtros MVP.

## Criterios de aceptación

- [x] **CA-1**: El dashboard muestra un resumen de temporada basado en las cosechas del Workspace y temporada activa o seleccionada.
- [x] **CA-2**: El widget de kg por destino usa la taxonomía cerrada del MVP, incluyendo `desconocido`.
- [x] **CA-3**: Los widgets se muestran sin error bloqueante incluso cuando algunos datos complementarios no existan.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| DashboardView - resumen | RN-005, RN-009 | cubierto | Vision General en una sola pantalla con scroll vertical; kg, litros y rendimiento medio ponderado |
| DashboardView - destino | RN-012 | cubierto | Kg por destino con la taxonomia cerrada e `desconocido` rotulado «Sin destino» |
| DashboardView - refresco | RN-006 | cubierto | Sin actualizacion en segundo plano; boton «Actualizar» explicito y aviso en pantalla |
| TemporadasView | — | cubierto | Produccion agregada por campana en la tarjeta del maestro (cierra `P-021`) |

## Notas y decisiones

- Esta historia cubre dos de los cuatro widgets MVP.
- **Cierra `P-021`**: la tarjeta de temporada muestra ya su produccion agregada, que `MVP-203` omitio
  deliberadamente por no inventar metricas sin datos de cosecha. Llega en una sola peticion
  (`GET /dashboard/kg-by-season`) y su fallo no tumba el maestro.
- **El resumen dice sobre cuantas partidas promedia.** Una media de rendimiento calculada sobre 2 de 20
  partidas, presentada a secas, se lee como la de la campana entera; y `null` en litros o rendimiento
  significa **desconocido**, no cero: «no salio aceite» seria una afirmacion falsa.
- **El ambito resuelto (RN-008) viaja en la respuesta.** Los defectos los pone el servidor —temporada
  activa y todos los terrenos activos—, asi que devolverlos es lo que permite a la pantalla explicar de
  que son las cifras, y a `MVP-405` posicionar los filtros sin duplicar la regla en el cliente.
- **Alcance que cierra `MVP-405`, no esta historia**: los filtros en la UI con su persistencia (RN-007)
  y el KPI `kg/arbol` con la exclusion de terrenos sin `num_arboles` (RN-010). El contrato publica
  `kg_per_tree` e `incomplete`, y aqui **no se emiten** a proposito: publicar un `kg/arbol` sin la
  exclusion de RN-010 seria publicar una cifra mal calculada. Detalle en el [tech-design](./tech-design.md).
