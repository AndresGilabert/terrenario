---
id: "MVP-203"
tipo: feature
titulo: "Maestro de temporadas y regla de única activa"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-201"]
bloquea: ["MVP-003", "MVP-004"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["temporadas"]
  modulo_path: "03-modulos/"
  componentes: ["temporadas"]
  etiquetas: ["mvp", "masters", "temporadas"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-203 — Maestro de temporadas y regla de única activa

## Contexto

La temporada es obligatoria para toda la operativa del MVP y debe existir una sola temporada activa por Workspace. Además, la KB cierra que una fecha fuera de rango se permite con aviso y que el estado `cerrada` es informativo, no bloqueante.

## Objetivo

Permitir gestionar temporadas del Workspace con una regla clara de única activa y con suficiente flexibilidad para no bloquear la operativa real.

## Requisitos de usuario

### HU-1 — Gestionar temporadas del Workspace

**Como** usuario del Workspace,
**quiero** crear y editar temporadas,
**para** usar la temporada como eje de organización y filtro del MVP.

### HU-2 — Mantener una única temporada activa

**Como** usuario operativo,
**quiero** que el sistema mantenga una sola temporada activa,
**para** evitar ambigüedad en autoselección y dashboards.

## Alcance (in-scope)

- Alta, edición y listado de temporadas.
- Regla de una única temporada activa por Workspace.
- Uso de estados `planificada`, `activa` y `cerrada`.
- Tratamiento de `cerrada` como estado informativo.
- Preparación para que la autoselección funcione en operativa diaria.

## Fuera de alcance (out-of-scope)

- Bloqueo fuerte por fechas fuera de rango.
- Modelado económico de campaña o precio de aceite.
- Gobernanza avanzada de temporadas históricas.

## Criterios de aceptación

- [x] **CA-1**: Un Workspace no puede tener más de una temporada activa al mismo tiempo. _(Índice único parcial `ux_seasons_workspace_active` + desbanque transaccional en `ActivateExclusivelyAsync`; verificado en SQLite y E2E sobre PostgreSQL.)_
- [x] **CA-2**: El usuario puede crear y editar temporadas sin perder flexibilidad operativa para registros fuera de rango. _(Alta/edición de nombre y fechas; la fecha de fin es opcional y no se bloquea por rango operativo; RN-023 se resolverá como aviso en las historias operativas.)_
- [x] **CA-3**: El estado `cerrada` no bloquea por sí mismo la edición o creación de registros posteriores. _(`cerrada` es informativo (RN-024); se puede reabrir, y no impide crear ni editar otras temporadas.)_

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/TemporadasView.tsx](../../../../../prototype/terrenario-mvp/src/components/TemporadasView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/OnboardingStep2.tsx](../../../../../prototype/terrenario-mvp/src/components/OnboardingStep2.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| TemporadasView | RN-022, RN-024 | cubierto | Maestro real (`components/seasons/TemporadasView.tsx`): listado con estados, alta, edición, cambio de activa y cierre/reapertura; verificado por UI conducida sobre PostgreSQL |
| OnboardingStep2 | RN-023 | fuera de alcance | El aviso de fecha fuera de rango es de las historias operativas (MVP-003/004); el maestro ya soporta fechas flexibles. Ver nota y P-017 (d) |

## Notas y decisiones

- La validación de fecha fuera de rango se gestionará como aviso en historias operativas, pero el maestro debe soportar ya esa regla.
