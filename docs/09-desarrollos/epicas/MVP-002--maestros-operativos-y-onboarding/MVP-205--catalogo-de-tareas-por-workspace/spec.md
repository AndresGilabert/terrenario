---
id: "MVP-205"
tipo: feature
titulo: "Catálogo de tareas por Workspace"
estado: completado
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
actualizado_en: "2026-07-28"
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

- [x] **CA-1**: Cada Workspace puede mantener su propio catálogo de tareas sin afectar al de otros Workspaces.
- [x] **CA-2**: El catálogo arranca vacío y puede poblarse sin configuración externa adicional.
- [x] **CA-3**: Las tareas con histórico pueden inactivarse sin invalidar registros que ya las utilicen.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| Catalogo tareas workspace (nuevo, sin prototipo) | RN-026 | cubierto | `/app/tareas`: alta y renombrado en linea, busqueda, filtro e inactivacion; verificado E2E (API+DB+UI) |
| ActivityModal | RN-025 | parcial | El catalogo reutilizable ya existe; la seleccion de tarea al registrar actividad es alcance de MVP-301 |

## Notas y decisiones

- La opción de guardar una tarea libre desde una actividad se implementará en la épica operativa, pero este maestro debe estar listo antes.
- **La prevención de duplicados se adelanta a esta historia** (decisión del PO, 2026-07-28). `MVP-302` la lleva en su alcance, pero la guarda pertenece al catálogo y no al flujo que lo alimenta: un maestro que admite «Poda» y «poda» contradice el motivo por el que existe (RN-026), y añadir el índice único después obligaría a una migración con limpieza de datos. Se implementa en dos niveles (guarda de aplicación + índice único sobre `(workspace_id, lower(name))`) y **MVP-302 la reutiliza** en vez de construirla. La normalización avanzada de nombres (acentos, similitud) sigue fuera de alcance en ambas.
- **Nueva entrada «Tareas» en el menú lateral** (decisión del PO, 2026-07-28), en `/app/tareas` y fuera de la guarda de oferta de temporada, como el resto de maestros de administración. La agrupación del menú por secciones queda registrada en `MVP-999` (P-025).
- El alta y el renombrado son **en línea**, sin modal: una tarea es un solo campo y poblar el catálogo consiste en escribir varias seguidas (ver `tech-design.md`).
