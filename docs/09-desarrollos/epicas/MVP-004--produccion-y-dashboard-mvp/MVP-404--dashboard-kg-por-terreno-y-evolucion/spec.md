---
id: "MVP-404"
tipo: feature
titulo: "Dashboard kg por terreno y evolución de rendimiento"
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
  dominios: ["dashboard", "kpis", "historico"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "kg-por-terreno", "evolucion-rendimiento"]
  etiquetas: ["mvp", "dashboard", "historico"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-30"
---

# MVP-404 — Dashboard kg por terreno y evolución de rendimiento

## Contexto

Los otros dos widgets mínimos del MVP son la distribución por terreno y la evolución de rendimiento. La KB fija reglas concretas de orden, visualización y comparativa histórica básica para que estas lecturas sean consistentes entre Workspaces y temporadas.

## Objetivo

Mostrar la distribución de kilos por terreno y la evolución temporal del rendimiento de forma consistente y útil para lectura rápida.

## Requisitos de usuario

### HU-1 — Comparar producción entre terrenos

**Como** usuario del Workspace,
**quiero** ver cuántos kg aporta cada terreno,
**para** comparar rápidamente el peso relativo de cada parcela en la temporada.

### HU-2 — Seguir la evolución del rendimiento

**Como** usuario que revisa la temporada,
**quiero** consultar la evolución del rendimiento y su referencia histórica básica,
**para** interpretar si la campaña actual se desvía del histórico disponible.

## Alcance (in-scope)

- Widget de kg por terreno.
- Ordenación por kg descendente y desempate alfabético.
- Widget de evolución de rendimiento.
- Comparativa histórica básica cuando haya suficiente dato.
- Uso de la unidad canónica L/100kg en la visualización de rendimiento.

## Fuera de alcance (out-of-scope)

- Ranking manual o personalizable de terrenos.
- Análisis histórico avanzado 5y/10y cuando no haya datos suficientes.
- Exploración ad-hoc de series complejas.

## Criterios de aceptación

- [x] **CA-1**: El widget de kg por terreno respeta el orden fijo definido por la KB.
- [x] **CA-2**: La evolución de rendimiento se muestra en unidad canónica y usa histórico básico solo cuando existe suficiente información.
- [x] **CA-3**: Ningún widget introduce agrupaciones o convenciones que contradigan las reglas de producto cerradas.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| DashboardView - kg por terreno | RN-011 | cubierto | Barras en orden fijo kg desc + desempate alfabetico, resuelto en servidor; verificado end-to-end |
| DashboardView - evolucion | RN-013, RN-015 | cubierto | Serie por mes/semana en L/100kg y comparativa historica basica presente solo con historico suficiente |

## Notas y decisiones

- Esta historia completa los cuatro widgets mínimos del dashboard MVP.
- **La comparativa histórica es una ventana de calendario, no campañas agrupadas** (decisión del PO,
  2026-07-30). El histórico son «los mismos días de años anteriores» a los de las cosechas de la campaña
  activa: el rango de fechas de esas cosechas —ensanchado una semana por lado para capturar más
  histórico— buscado en cada año anterior. Una cosecha de otra época del año queda fuera por no ser
  comparable, y el filtro de terreno viaja al histórico, así que se comparan las mismas parcelas. El
  «histórico suficiente» (CA-2) se mide por profundidad: la media general con un año previo con dato;
  la de 5 años solo si el histórico llega 5 años atrás, y la de 10 si llega 10.
- **El histórico aparece aunque la campaña activa no tenga cosechas todavía** (petición del PO): al
  empezar una campaña no hay línea actual, pero sí interesa ver a cuánto rindieron esas fechas otros
  años. Sin cosechas, la ventana la fija el calendario de la propia temporada.
- **Un periodo sin dato de aceite no dibuja punto**, igual que `null` ≠ 0 en el resumen: forzar un cero
  fingiría una caída que no ocurrió.
- **El orden de RN-011 se resuelve en servidor** —«no hay orden manual» es parte de la regla— y kg por
  terreno excluye los terrenos que no produjeron, como kg por destino con las categorías vacías.
- **Alcance que cierra `MVP-405`**: los filtros de temporada y terrenos en la UI con su persistencia
  (RN-007) y el KPI `kg/árbol` con la exclusión de RN-010. El backend ya acepta `season_id`/`plot_ids`
  y devuelve el `scope` resuelto en los cuatro endpoints. Detalle en el [tech-design](./tech-design.md).
