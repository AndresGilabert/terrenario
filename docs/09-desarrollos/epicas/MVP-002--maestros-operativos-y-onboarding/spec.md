---
id: "MVP-002"
tipo: epica
titulo: "Maestros operativos y onboarding"
estado: borrador
prioridad: alta
hito: "Hito B — Base operativa preparada"
tickets: []
historias: ["MVP-201", "MVP-202", "MVP-203", "MVP-204", "MVP-205"]
depende_de: ["MVP-001"]
bloquea: ["MVP-003", "MVP-004"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["terrenos", "temporadas", "trabajadores", "tareas"]
  modulo_path: "03-modulos/"
  componentes: ["terrenos", "temporadas", "trabajadores", "tareas"]
  etiquetas: ["mvp", "masters", "onboarding"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# EPICA MVP-002 — Maestros operativos y onboarding

## Contexto

La operativa diaria y la producción dependen de una base mínima de datos maestros. La KB ya cerró que el alta de terrenos debe ser ligera, que la temporada es obligatoria con una sola activa por Workspace, que los miembros del Workspace aparecen como trabajadores seleccionables y que el catálogo de tareas es editable por Workspace.

Sin este bloque, el diario y las cosechas arrancarían con semántica inestable o con demasiada fricción manual.

## Objetivo

Dejar cada Workspace preparado para empezar a registrar actividad real en pocos pasos, con maestros mínimos coherentes y sin configuración avanzada inicial.

## Requisitos de usuario de alto nivel

- **Como** usuario que acaba de crear un Workspace, **quiero** disponer rápidamente de terrenos, temporada y responsables, **para** empezar a registrar operativa sin preparar el sistema durante mucho tiempo.
- **Como** usuario recurrente, **quiero** reutilizar tareas y responsables consistentes, **para** evitar errores y duplicidades en el registro diario.

## Alcance

- CRUD básico de terrenos con alta mínima `nombre` + `tipo_propiedad`.
- CRUD de temporadas con una única temporada activa por Workspace.
- Creación automática o propuesta de primera temporada al crear Workspace.
- Maestro de trabajadores con mezcla de miembros del Workspace y trabajadores sin cuenta.
- Catálogo de tareas editable por Workspace, inicialmente vacío.
- Política de inactivación de tareas, terrenos y trabajadores con histórico.

## Fuera de alcance

- Geolocalización avanzada, mapas o validación fuerte de referencia catastral.
- Historización de número de árboles por temporada.
- Taxonomías globales de tareas por cultivo.
- Gestión avanzada de ownership o permisos por maestro.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [ ] **CA-2**: Un Workspace nuevo puede arrancar con primera temporada y los maestros mínimos necesarios sin configuración técnica adicional.
- [ ] **CA-3**: Actividades, compras y cosechas pueden depender exclusivamente de estos maestros sin recurrir a texto libre salvo donde el MVP lo permite explícitamente.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-201` — Onboarding inicial del Workspace y primera temporada.
- `MVP-202` — Maestro de terrenos con alta mínima.
- `MVP-203` — Maestro de temporadas y regla de única activa.
- `MVP-204` — Maestro de trabajadores y exposición automática de miembros.
- `MVP-205` — Catálogo de tareas por Workspace.

## Notas y decisiones

- `num_arboles` es opcional en MVP y su ausencia debe reflejarse después como dato incompleto en dashboard.
- La temporada cerrada es informativa, no bloqueante.
- El catálogo de tareas por Workspace es parte del MVP base, no de una fase media.
