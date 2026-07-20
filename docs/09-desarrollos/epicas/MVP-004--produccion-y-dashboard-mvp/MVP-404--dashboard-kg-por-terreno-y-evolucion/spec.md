---
id: "MVP-404"
tipo: feature
titulo: "Dashboard kg por terreno y evolución de rendimiento"
estado: borrador
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
actualizado_en: "2026-07-20"
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

- [ ] **CA-1**: El widget de kg por terreno respeta el orden fijo definido por la KB.
- [ ] **CA-2**: La evolución de rendimiento se muestra en unidad canónica y usa histórico básico solo cuando existe suficiente información.
- [ ] **CA-3**: Ningún widget introduce agrupaciones o convenciones que contradigan las reglas de producto cerradas.

## Maquetas y referencias visuales

- Referencia funcional: RN-011 y RN-015.

## Notas y decisiones

- Esta historia completa los cuatro widgets mínimos del dashboard MVP.
