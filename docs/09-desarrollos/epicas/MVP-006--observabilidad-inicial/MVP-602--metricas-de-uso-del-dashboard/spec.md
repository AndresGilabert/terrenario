---
id: "MVP-602"
tipo: feature
titulo: "Métricas de uso del dashboard"
estado: completado
prioridad: media
sprint: ""
hito: "Hito F — Operación medible"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-006--observabilidad-inicial"
depende_de: ["MVP-403", "MVP-404", "MVP-405", "MVP-504"]
bloquea: ["MVP-603"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["observabilidad", "dashboard"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard-usage", "manual-refresh"]
  etiquetas: ["mvp", "telemetry", "dashboard"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-08-06"
---

# MVP-602 — Métricas de uso del dashboard

## Contexto

El dashboard es la principal promesa de visibilidad del MVP. La KB define KPIs de uso del dashboard y de recarga manual que deben medirse para saber si el producto realmente se consulta y cómo se usa.

## Objetivo

Disponer de señales básicas de uso del dashboard y recarga manual que permitan evaluar adopción y fricción de lectura en los primeros usuarios.

## Requisitos de usuario

### HU-1 — Medir uso del dashboard

**Como** responsable del producto,
**quiero** saber si los usuarios acceden al dashboard y lo recargan,
**para** validar que la capa de visibilidad aporta valor real en el MVP.

## Alcance (in-scope)

- Métricas de acceso al dashboard.
- Señal de recarga manual del dashboard.
- Base mínima para revisión semanal de KPIs de producto.

## Fuera de alcance (out-of-scope)

- Analítica profunda por widget o comportamiento avanzado.
- Heatmaps o tracking exhaustivo de interfaz.

## Criterios de aceptación

- [x] **CA-1**: El sistema registra el acceso al dashboard de forma suficiente para medir sesiones con uso del panel.
- [x] **CA-2**: El sistema registra la recarga manual del dashboard como señal separada.
- [x] **CA-3**: Las métricas permiten revisar los KPIs mínimos de uso definidos en la KB.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/TopNavbar.tsx](../../../../../prototype/terrenario-mvp/src/components/TopNavbar.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| DashboardView | RN-006, RN-007 | cubierto | Entrada, recarga manual y cobertura de los cuatro widgets, verificadas en navegador: el clic en «Actualizar» emite `dashboard_manual_refresh` y el cambio de temporada no |
| TopNavbar | RN-006 | cubierto | La sesión activa se cuenta en el shell (`AppLayout`), que es el divisor del KPI |

## Notas y decisiones

- Esta historia mide adopción mínima, no comportamiento exhaustivo.
- Se miden **sesiones**, no visitas: quien entra ocho veces al panel en una sesión sigue siendo una
  sesión, y contar visitas daría porcentajes por encima del 100 %.
- El estado **vacío de un widget cuenta como cubierto**: el KPI de la KB lo admite expresamente, y
  tratarlo como fallo haría bajar la cobertura con cada Workspace nuevo.
- Al ampliar la medición más allá del acceso se rehace la evaluación de exención de `RN-042` y se
  actualiza lo publicado (Política de Privacidad y panel de Ajustes).
- El detalle técnico y las alternativas descartadas están en [tech-design.md](./tech-design.md).
