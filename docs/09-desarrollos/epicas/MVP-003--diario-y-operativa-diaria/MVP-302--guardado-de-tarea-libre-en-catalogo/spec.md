---
id: "MVP-302"
tipo: feature
titulo: "Guardado de tarea libre en catálogo"
estado: borrador
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
actualizado_en: "2026-07-21"
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
- Prevención básica de duplicados evidentes dentro del mismo Workspace.

## Fuera de alcance (out-of-scope)

- Normalización avanzada de nombres de tareas.
- Sugerencias automáticas por similitud compleja.
- Catálogo compartido entre Workspaces.

## Criterios de aceptación

- [ ] **CA-1**: Una tarea introducida en texto libre puede guardarse desde el flujo de actividad sin salir del contexto de trabajo.
- [ ] **CA-2**: La tarea guardada queda disponible en el catálogo del Workspace activo para usos posteriores.
- [ ] **CA-3**: La operación no afecta a otros Workspaces ni rompe la actividad ya registrada.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| ActivityModal | RN-026 | falta | No existe opcion de guardar tarea libre al catalogo |
| ActivityModal | RN-025 | parcial | Existe captura de texto libre de actividad |

## Notas y decisiones

- Esta historia mejora consistencia y velocidad, pero no bloquea el registro básico de actividades.
