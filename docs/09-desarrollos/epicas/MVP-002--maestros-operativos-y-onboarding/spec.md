---
id: "MVP-002"
tipo: epica
titulo: "Maestros operativos y onboarding"
estado: borrador
prioridad: alta
hito: "Hito B — Base operativa preparada"
tickets: []
historias: ["MVP-201", "MVP-202", "MVP-203", "MVP-204", "MVP-205", "MVP-206", "MVP-207", "MVP-299"]
depende_de: ["MVP-001"]
bloquea: ["MVP-003", "MVP-004"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["terrenos", "temporadas", "trabajadores", "tareas", "workspaces"]
  modulo_path: "03-modulos/"
  componentes: ["terrenos", "temporadas", "trabajadores", "tareas", "workspaces"]
  etiquetas: ["mvp", "masters", "onboarding"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-28"
---

# EPICA MVP-002 — Maestros operativos y onboarding

## Contexto

La operativa diaria y la producción dependen de una base mínima de datos maestros. La KB ya cerró que el alta de terrenos debe ser ligera, que la temporada es obligatoria con una sola activa por Workspace, que los miembros del Workspace aparecen como trabajadores seleccionables y que el catálogo de tareas es editable por Workspace.

Sin este bloque, el diario y las cosechas arrancarían con semántica inestable o con demasiada fricción manual.

## Objetivo

Dejar cada Workspace preparado para empezar a registrar actividad real en pocos pasos, con maestros mínimos coherentes y sin configuración avanzada inicial, y con el ciclo de vida del propio Workspace cerrado para que esa preparación no se pierda ni quede huérfana.

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
- **Unicidad de nombre por Workspace en los maestros** (nombres no duplicables ignorando
  mayúsculas), condición para que el maestro cumpla su función de referencia estable.
- **Absorbido durante la épica**: **ciclo de vida del Workspace** (renombrar, baja lógica,
  propiedad y reactivación), entregado en `MVP-206`. Ver Notas.

## Fuera de alcance

- Geolocalización avanzada, mapas o validación fuerte de referencia catastral.
- Historización de número de árboles por temporada.
- Taxonomías globales de tareas por cultivo.
- Permisos granulares por maestro (siguen siendo planos, RN-034). La propiedad del Workspace sí
  entra, vía `MVP-206`, pero solo para las reglas de no-orfandad y traspaso (RN-038).
- Normalización avanzada de nombres (acentos, similitud) en la unicidad de los maestros.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [ ] **CA-2**: Un Workspace nuevo puede arrancar con primera temporada y los maestros mínimos necesarios sin configuración técnica adicional.
- [ ] **CA-3**: Actividades, compras y cosechas pueden depender exclusivamente de estos maestros sin recurrir a texto libre salvo donde el MVP lo permite explícitamente.
- [ ] **CA-4**: Los maestros son una referencia estable: dentro de un Workspace no conviven dos registros del mismo maestro con el mismo nombre, y su contrato publicado describe la API realmente entregada.
- [ ] **CA-5**: El Workspace que sostiene esos maestros tiene su ciclo de vida cerrado: se puede renombrar y dar de baja de forma reversible, y nunca queda sin propietario (RN-038/RN-039/RN-040).

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-201` — Onboarding inicial del Workspace y primera temporada.
- `MVP-202` — Maestro de terrenos con alta mínima.
- `MVP-203` — Maestro de temporadas y regla de única activa.
- `MVP-204` — Maestro de trabajadores y exposición automática de miembros.
- `MVP-205` — Catálogo de tareas por Workspace.
- `MVP-206` — Ciclo de vida del Workspace: renombrar, baja lógica y traspaso de propiedad.
- `MVP-207` — Correcciones de cierre de la épica de maestros.
- `MVP-299` — Revision epica.

## Vinculacion con prototipo (fuente visual)

Regla de precedencia para todas las historias de esta epica:

- La fuente de verdad funcional y de requisitos es la KB.
- El prototipo solo aporta referencia visual, estructura de pantallas y flujos UX.
- Si hay contradiccion, prevalece la KB.

Referencia base del prototipo:

- [prototype/terrenario-mvp/src/components/OnboardingStep1.tsx](../../../../prototype/terrenario-mvp/src/components/OnboardingStep1.tsx)
- [prototype/terrenario-mvp/src/components/OnboardingStep2.tsx](../../../../prototype/terrenario-mvp/src/components/OnboardingStep2.tsx)
- [prototype/reports/mvp-prototype-coverage.md](../../../../prototype/reports/mvp-prototype-coverage.md)

Matriz historia -> pantallas/componentes:

| Historia | Referencias de prototipo | Cobertura |
|---|---|---|
| MVP-201 | [prototype/terrenario-mvp/src/components/OnboardingStep1.tsx](../../../../prototype/terrenario-mvp/src/components/OnboardingStep1.tsx), [prototype/terrenario-mvp/src/components/OnboardingStep2.tsx](../../../../prototype/terrenario-mvp/src/components/OnboardingStep2.tsx) | Parcial: alta visual de Workspace/temporada disponible |
| MVP-202 | [prototype/terrenario-mvp/src/components/TerrenosView.tsx](../../../../prototype/terrenario-mvp/src/components/TerrenosView.tsx), [prototype/terrenario-mvp/src/components/TerrenoModal.tsx](../../../../prototype/terrenario-mvp/src/components/TerrenoModal.tsx), [prototype/terrenario-mvp/src/components/TerrenoDetailModal.tsx](../../../../prototype/terrenario-mvp/src/components/TerrenoDetailModal.tsx) | Parcial: CRUD visual de terrenos disponible |
| MVP-203 | [prototype/terrenario-mvp/src/components/TemporadasView.tsx](../../../../prototype/terrenario-mvp/src/components/TemporadasView.tsx) | Parcial: gestion y activacion visual de temporada; reglas completas de validacion/rango pendientes |
| MVP-204 | [prototype/terrenario-mvp/src/components/TrabajadoresView.tsx](../../../../prototype/terrenario-mvp/src/components/TrabajadoresView.tsx), [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx) | Parcial: maestro de trabajadores y seleccion en actividad disponibles |
| MVP-205 | [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx) | No cubierto funcionalmente: no existe catalogo de tareas por Workspace ni inactivacion |
| MVP-206 | [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx) | No cubierto: renombrar/baja logica/traspaso de propiedad no existen en el prototipo |
| MVP-207 | [prototype/terrenario-mvp/src/components/TemporadasView.tsx](../../../../prototype/terrenario-mvp/src/components/TemporadasView.tsx), [prototype/terrenario-mvp/src/components/TrabajadoresView.tsx](../../../../prototype/terrenario-mvp/src/components/TrabajadoresView.tsx) | No aplica: correcciones sobre pantallas ya entregadas por MVP-201..205 |

## Notas y decisiones

- `num_arboles` es opcional en MVP y su ausencia debe reflejarse después como dato incompleto en dashboard.
- La temporada cerrada es informativa, no bloqueante.
- El catálogo de tareas por Workspace es parte del MVP base, no de una fase media.
- **Alcance absorbido durante la épica (trazado, no defecto).** El **ciclo de vida del Workspace**
  entró vía `MVP-206` desde el punto `P-004` de `MVP-999`, replanteado durante `MVP-204`: renombrado,
  baja lógica, traspaso de propiedad, notificación por email y reactivación autorizada, con las
  reglas nuevas **RN-038/RN-039/RN-040**. No es un maestro operativo, pero sostiene a todos: sin él,
  un Workspace creado por error o abandonado por su propietario no tenía salida. El objetivo, el
  alcance y los criterios de aceptación de esta épica se ampliaron en la revisión de cierre
  (`MVP-299`, hallazgo R-03) para reflejarlo, con el mismo criterio con el que `MVP-001` documentó
  lo absorbido por `MVP-107`.
- **Correcciones de cierre.** La revisión `MVP-299` (2026-07-28) verificó las seis historias contra
  la API real y la UI conducida: todas conformes. Los defectos detectados sobre lo entregado
  (contrato de temporadas desalineado, ausencia de guarda de duplicados fuera del catálogo de
  tareas, invitación pendiente no anulable y dos incoherencias de acceso) se agrupan en `MVP-207`.
  La épica no cierra hasta entregarla.
