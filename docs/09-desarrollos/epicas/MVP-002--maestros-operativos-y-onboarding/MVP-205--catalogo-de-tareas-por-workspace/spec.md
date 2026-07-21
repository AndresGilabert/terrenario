---
id: "MVP-205"
tipo: feature
titulo: "Catálogo de tareas por Workspace"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-201"]
bloquea: ["MVP-003"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["tareas", "actividades"]
  modulo_path: "03-modulos/"
  componentes: ["tareas"]
  etiquetas: ["mvp", "masters", "tareas"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-205 — Catálogo de tareas por Workspace

## Contexto

La KB cierra que la tarea es obligatoria en actividades y que el catálogo de tareas es editable por Workspace, inicialmente vacío y compatible con texto libre guardable. Este maestro debe existir antes de abrir la épica de diario y operativa diaria.

## Objetivo

Permitir que cada Workspace mantenga su propio catálogo de tareas reutilizables para mejorar consistencia y velocidad de registro posterior.

## Requisitos de usuario

### HU-1 — Mantener tareas reutilizables

**Como** usuario del Workspace,
**quiero** crear y mantener tareas propias,
**para** reutilizarlas después en el registro diario.

### HU-2 — Evitar borrar tareas con histórico

**Como** usuario que mantiene el catálogo,
**quiero** inactivar tareas que ya no use,
**para** limpiar el catálogo sin romper registros previos.

## Alcance (in-scope)

- Alta, edición, listado e inactivación de tareas por Workspace.
- Catálogo inicial vacío por Workspace.
- Preparación para que una tarea libre pueda guardarse después desde operativa diaria.
- Cohesión con el selector de Workspace activo.

## Fuera de alcance (out-of-scope)

- Catálogo global compartido entre Workspaces.
- Sugerencias automáticas por época o recurrencia.
- Jerarquías o clasificaciones complejas de tareas.

## Criterios de aceptación

- [ ] **CA-1**: Cada Workspace puede mantener su propio catálogo de tareas sin afectar al de otros Workspaces.
- [ ] **CA-2**: El catálogo arranca vacío y puede poblarse sin configuración externa adicional.
- [ ] **CA-3**: Las tareas con histórico pueden inactivarse sin invalidar registros que ya las utilicen.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| ActivityModal | RN-025 | parcial | Entrada de actividad y titulo disponibles |
| Catalogo tareas workspace | RN-026 | falta | No existe pantalla/catalogo de tareas reutilizable |

## Notas y decisiones

- La opción de guardar una tarea libre desde una actividad se implementará en la épica operativa, pero este maestro debe estar listo antes.
