---
id: "MVP-302"
tipo: feature
titulo: "Guardado de tarea libre en catálogo"
estado: completado
prioridad: media
sprint: ""
hito: "Hito C — Registro operativo end-to-end"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-003--diario-y-operativa-diaria"
depende_de: ["MVP-301", "MVP-205"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["actividades", "tareas"]
  modulo_path: "03-modulos/"
  componentes: ["actividades", "tareas"]
  etiquetas: ["mvp", "tareas", "catalogo"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-29"
---

# MVP-302 — Guardado de tarea libre en catálogo

## Contexto

El modelo funcional del MVP permite introducir una tarea en texto libre cuando todavía no existe en el catálogo del Workspace, pero también exige que el sistema pueda aprender de esa captura y ofrecer guardarla para usos futuros.

## Objetivo

Permitir que una tarea libre registrada durante una actividad se pueda convertir fácilmente en tarea reutilizable del catálogo del Workspace.

## Requisitos de usuario

### HU-1 — Reutilizar una tarea libre creada al vuelo

**Como** usuario que registra una actividad,
**quiero** guardar una tarea libre en el catálogo del Workspace,
**para** no tener que volver a escribirla en registros futuros.

## Alcance (in-scope)

- Opción de guardar una tarea libre introducida en una actividad.
- Alta de esa tarea en el catálogo del Workspace activo.
- **Reutilización** de la guarda de duplicados ya entregada por `MVP-205` (guarda de aplicación más
  el índice único `ux_tasks_workspace_name` sobre `(workspace_id, lower(name))`): esta historia
  **no la construye**, solo trata su `409 CONFLICT_TASK_NAME_DUPLICATE` desde el flujo de actividad,
  ofreciendo reutilizar la tarea ya existente en vez de crear una segunda.

## Fuera de alcance (out-of-scope)

- Normalización avanzada de nombres de tareas.
- Sugerencias automáticas por similitud compleja.
- Catálogo compartido entre Workspaces.

## Criterios de aceptación

- [x] **CA-1**: Una tarea introducida en texto libre puede guardarse desde el flujo de actividad sin salir del contexto de trabajo.
- [x] **CA-2**: La tarea guardada queda disponible en el catálogo del Workspace activo para usos posteriores.
- [x] **CA-3**: La operación no afecta a otros Workspaces ni rompe la actividad ya registrada.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| ActivityModal | RN-026 | cubierto | Casilla «Guardar esta tarea en el catalogo del Workspace» bajo el campo de texto libre, con aviso en linea de si se creara, reutilizara o reactivara. Verificado en UI conducida |
| ActivityModal | RN-025 | cubierto | Captura de texto libre y seleccion desde el catalogo, excluyentes (MVP-301) |
| DiarioView | RN-026 | cubierto | Accion `playlist_add` en las tarjetas de tarea libre para promocionar una actividad **ya registrada** sin reescribirla (CA-3) |

## Notas y decisiones

- Esta historia mejora consistencia y velocidad, pero no bloquea el registro básico de actividades.
- **Ajuste de alcance (revisión de cierre de MVP-002, `MVP-299`, hallazgo R-14).** La prevención de
  duplicados **se adelantó a `MVP-205`** por decisión del PO (`MVP-999`, P-026): la guarda pertenece
  al catálogo, no al flujo que lo alimenta. Esta historia pasa a **reutilizarla**, no a construirla.
  La normalización avanzada de nombres (acentos, similitud) sigue fuera de alcance en ambas.
- **Cómo se reutiliza la guarda (decisión de implementación, 2026-07-29).** En vez de intentar el alta
  y tratar el `409`, se consulta **la misma comparación** que sostiene el índice único para *resolver*
  el nombre: si la tarea ya existe se reutiliza, y si estaba inactivada se reactiva (`MVP-205`, CA-3).
  Motivo: en el flujo de actividad un `409` no es accionable —el usuario no tiene nada que arreglar y
  lo único que quiere es que la labor quede apuntada—, mientras que en `POST /tasks`, donde el alta es
  el objetivo, sigue teniendo sentido. La respuesta informa de lo ocurrido con `task_catalog_outcome`
  (`created`/`reused`/`reactivated`) para que la UI no diga «guardado» cuando no ha creado nada.
- **Dos puntos de entrada.** Durante la captura (casilla en el formulario, CA-1) y sobre una actividad
  **ya registrada** (acción en su tarjeta del diario, CA-3), esta última con
  `PATCH { save_task_to_catalog: true }` a secas, sin reescribir el texto.
