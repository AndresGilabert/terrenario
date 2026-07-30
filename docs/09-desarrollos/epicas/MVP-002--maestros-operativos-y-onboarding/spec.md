---
id: "MVP-002"
tipo: epica
titulo: "Maestros operativos y onboarding"
estado: completado
prioridad: alta
hito: "Hito B — Base operativa preparada"
tickets: []
historias: ["MVP-201", "MVP-202", "MVP-203", "MVP-204", "MVP-205", "MVP-206", "MVP-207", "MVP-208", "MVP-209", "MVP-299"]
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
actualizado_en: "2026-07-30"
---

# EPICA MVP-002 — Maestros operativos y onboarding

## Contexto

La operativa diaria y la producción dependen de una base mínima de datos maestros. La KB ya cerró que el alta de terrenos debe ser ligera, que la temporada es obligatoria con una sola activa por Workspace, que los miembros del Workspace aparecen como trabajadores seleccionables y que el catálogo de tareas es editable por Workspace.

Sin este bloque, el diario y las cosechas arrancarían con semántica inestable o con demasiada fricción manual.

## Objetivo

Dejar cada Workspace preparado para empezar a registrar actividad real en pocos pasos, con maestros mínimos coherentes y sin configuración avanzada inicial, con el ciclo de vida del propio Workspace cerrado para que esa preparación no se pierda ni quede huérfana, y con cada maestro **referenciable sin ambigüedad** por la operativa diaria que vendrá después.

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
  mayúsculas), condición para que el maestro cumpla su función de referencia estable. La unicidad
  aplica al maestro completo, incluida la frontera miembro/cuadrilla del maestro de responsables.
- **Identidad única de responsable**: todo miembro del Workspace y todo trabajador sin cuenta
  comparten un único espacio de identificadores, para que la operativa diaria pueda referenciar a
  cualquiera de los dos sin campos alternativos. Entregado en `MVP-208`. Ver Notas.
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

- [x] **CA-1**: Todas las historias de la épica están en estado `completado`. _(10/10 en `_indice.md`:
  9/9 tras cerrar `MVP-299` en su 3ª pasada, más `MVP-209` como corrección de modelo posterior al
  cierre; ver Notas.)_
- [x] **CA-2**: Un Workspace nuevo puede arrancar con primera temporada y los maestros mínimos necesarios sin configuración técnica adicional. _(Verificado en la 1ª pasada sobre un Workspace creado para la prueba y ratificado en la 3ª.)_
- [x] **CA-3**: Actividades, compras y cosechas pueden depender exclusivamente de estos maestros sin recurrir a texto libre salvo donde el MVP lo permite explícitamente. Incluye al **responsable**: tanto un miembro del Workspace como un trabajador sin cuenta se identifican con un único tipo de referencia y pueden guardarse (`MVP-208`). _(3ª pasada: `GET /workers` devuelve un único espacio de identificadores con `kind`, y todo miembro activo tiene su `workers.id`, así que `ACTIVITY.worker_id` sigue siendo una FK simple. El texto libre que queda —producto de compra (RN-031)— está permitido explícitamente; el producto y el destino de cosecha son catálogo global fijo (RN-030/RN-012), alcance de `MVP-402`.)_
- [x] **CA-4**: Los maestros son una referencia estable: dentro de un Workspace no conviven dos registros del mismo maestro con el mismo nombre —tampoco a través de la frontera miembro/cuadrilla del maestro de responsables—, y su contrato publicado describe la API realmente entregada, tanto en el alta como en la edición. _(3ª pasada: `409` verificado en los cuatro maestros y cruzando miembro/cuadrilla, con mayúsculas y espacios sobrantes, garantizado por los índices únicos; contrato verificado fila a fila contra la API tras cerrar `R-18` en `MVP-208` y `R-24` en `MVP-299`.)_
- [x] **CA-5**: El Workspace que sostiene esos maestros tiene su ciclo de vida cerrado: se puede renombrar y dar de baja de forma reversible, y nunca queda sin propietario (RN-038/RN-039/RN-040). _(Verificado end-to-end en la 1ª pasada; la incoherencia que la rama de reasignación dejaba en el maestro de responsables (`R-25`) se corrigió en la 3ª.)_

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-201` — Onboarding inicial del Workspace y primera temporada.
- `MVP-202` — Maestro de terrenos con alta mínima.
- `MVP-203` — Maestro de temporadas y regla de única activa.
- `MVP-204` — Maestro de trabajadores y exposición automática de miembros.
- `MVP-205` — Catálogo de tareas por Workspace.
- `MVP-206` — Ciclo de vida del Workspace: renombrar, baja lógica y traspaso de propiedad.
- `MVP-207` — Correcciones de cierre de la épica de maestros.
- `MVP-208` — Identidad del responsable y correcciones finales de la épica de maestros.
- `MVP-209` — Estado de temporada y temporada de trabajo por usuario (corrección de modelo posterior al cierre; cierra `P-045`).
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
| MVP-208 | [prototype/terrenario-mvp/src/components/TrabajadoresView.tsx](../../../../prototype/terrenario-mvp/src/components/TrabajadoresView.tsx), [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx) | No aplica: modelo del maestro de responsables y correcciones sobre pantallas ya entregadas por MVP-201..207 |
| MVP-209 | [prototype/terrenario-mvp/src/components/TemporadasView.tsx](../../../../prototype/terrenario-mvp/src/components/TemporadasView.tsx) | No aplica: corrección de modelo (estado ≠ temporada de trabajo por usuario) sobre el maestro ya entregado por MVP-203 |

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
- **Correcciones de cierre (1ª pasada).** La revisión `MVP-299` (2026-07-28) verificó las seis
  historias contra la API real y la UI conducida: todas conformes. Los defectos detectados sobre lo
  entregado (contrato de temporadas desalineado, ausencia de guarda de duplicados fuera del catálogo
  de tareas, invitación pendiente no anulable y dos incoherencias de acceso) se agruparon en
  `MVP-207`, ya entregada.
- **Alcance ampliado en la 2ª pasada (trazado, no defecto): la identidad del responsable.** La
  segunda pasada de `MVP-299` (2026-07-28), sobre `MVP-207` ya entregada, encontró que el **CA-3 de
  esta épica no se cumplía**: `ACTIVITY.worker_id` es una FK a `workers` pero, por la decisión de
  `MVP-204`, los miembros del Workspace no son filas de `workers`, así que un miembro elegido como
  responsable no se podía guardar (`MVP-999`, `P-034`). De la misma decisión salía que la guarda de
  nombre único de `MVP-207` no cubría la frontera miembro/cuadrilla (hallazgo `R-16`).
  **Decisión del PO (2026-07-28): no arrastrar la incidencia.** `P-034` se reasigna de `MVP-301` a
  esta épica y se resuelve en `MVP-208` materializando una fila de `workers` por miembro, lo que
  cierra a la vez el CA-3 y la parte de `R-16` del CA-4. `MVP-208` recoge además el resto de defectos
  de la 2ª pasada (`R-15`, `R-17`, `R-18` documental, `R-20`, `R-21`). El objetivo, el alcance y los
  criterios de aceptación se ampliaron aquí para reflejarlo, con el mismo criterio con el que se
  documentó lo absorbido por `MVP-206`.
- **Cierre de la épica (3ª pasada de `MVP-299`, 2026-07-28).** Las ocho historias quedan entregadas y
  verificadas contra la API real y la UI conducida, y los **cinco criterios de la épica se cumplen**.
  La pasada encontró dos defectos de lo entregado —`R-25`, la baja de Workspace con copropietarios
  revocaba el acceso sin retirar a esa persona del maestro de responsables, y `R-24`, cinco filas del
  contrato del alta que la API no cumplía— y, **por ser correcciones menores de lo ya prometido por
  `MVP-208`, el PO decide resolverlas en la propia `MVP-299`** en vez de abrir una novena historia,
  con criterios de aceptación propios (`CA-4`/`CA-5` de esa historia) para que queden verificadas. Lo
  demás se difiere a `MVP-999`: `P-048` (un miembro no propietario no puede abandonar un Workspace),
  `P-049` (`can_revoke` frente a la guarda real con varios propietarios) y `P-050` (el ER de
  `PURCHASE` sin `season_id`, con destino `MVP-303`). Ninguno rompe un criterio de esta épica ni
  bloquea a `MVP-003`/`MVP-004`.
- **Corrección de modelo posterior al cierre (`MVP-209`, 2026-07-30).** Al construir el filtro de
  temporada del dashboard (`MVP-405`) afloró `P-045`: una campaña **pasada** desbancada por «crear
  cambia la activa» (`P-017`) quedaba `is_active=false, is_closed=false` y se rotulaba «planificada»,
  que describe algo por venir. Al plantearlo, el PO reformuló el problema de fondo: el modelo **fundía
  dos conceptos** en el único `Season.is_active` —el **estado** informativo de la campaña y la
  **temporada de trabajo** sobre la que se registra— y además la de trabajo era global por Workspace,
  de modo que un usuario cambiaba la de otro. `MVP-209` los separa: el **estado** (`planificada`/
  `abierta`/`cerrada`) se deriva de `is_closed` + fechas —`abierta` cubre las pasadas no cerradas— y la
  **temporada de trabajo** pasa a ser **por usuario** (`workspace_members.active_season_id`). Es una
  corrección del maestro de temporadas de esta épica (Hito B ya promocionado), no un defecto de una
  historia concreta; se documenta aquí con el mismo criterio que lo absorbido por `MVP-206`/`MVP-208`,
  cierra `P-045` y desbloquea el filtro de `MVP-405`. Decisión del PO (2026-07-30): **priorizar el
  rediseño completo ya**, antes de cerrar `MVP-405`.
