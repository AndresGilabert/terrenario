---
id: "MVP-208"
tipo: feature
titulo: "Identidad del responsable y correcciones finales de la épica de maestros"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "5d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-201", "MVP-203", "MVP-204", "MVP-205", "MVP-207"]
bloquea: ["MVP-301"]
relacionado_con: ["MVP-299", "MVP-206", "MVP-301"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["trabajadores", "workspaces", "temporadas", "invitaciones", "contratos"]
  modulo_path: "03-modulos/"
  componentes: ["trabajadores", "workspace-members", "invitaciones", "temporadas", "app-shell"]
  etiquetas: ["mvp", "masters", "correccion", "modelo", "cierre-epica"]
  nivel_riesgo: alto
creado_en: "2026-07-28"
actualizado_en: "2026-07-28"
---

# MVP-208 — Identidad del responsable y correcciones finales de la épica de maestros

## Contexto

La segunda pasada de la revisión de cierre (`MVP-299`, 2026-07-28) verificó `MVP-207` contra la API
real y la UI conducida: sus seis criterios están entregados, con dos huecos (el canal `enlace` y la
ruta de alta del contrato). Pero la misma pasada dejó al descubierto que **el CA-3 de la épica no se
cumple**: «actividades, compras y cosechas pueden depender exclusivamente de estos maestros».

El maestro de responsables es el que falla. `ACTIVITY.worker_id` es una FK a `workers` y el contrato
exige `worker_id*`, pero `MVP-204` decidió que los miembros del Workspace **no** son filas de
`workers`: se exponen desde `workspace_members`, identificados por `user_id`. RN-027 obliga a que
todo miembro sea seleccionable como responsable, así que hoy **un miembro elegido como responsable no
se puede guardar**, y `TrabajadoresView` combina en cliente dos listados con espacios de
identificadores distintos. Es el punto `P-034`, registrado con destino `MVP-301`.

De esa misma decisión sale un segundo defecto, detectado ahora: la guarda de nombre único de
`MVP-207` es **por tabla**, no sobre la unión que RN-027 define como maestro. Con el miembro «Andrés
Gilabert» en el Workspace, `POST /workers` con ese mismo nombre responde `201` y la pantalla muestra
dos personas indistinguibles (hallazgo `R-16`). Es exactamente el motivo por el que existe HU-1 de
`MVP-204` («evitar nombres duplicados o inconsistentes»).

**Decisión del PO (2026-07-28): no arrastrar la incidencia.** `P-034` y `R-16` se resuelven **dentro
de MVP-002**, en esta historia, en vez de diferirse a `MVP-301`. Resolver la identidad del
responsable cierra el CA-3 de la épica y, al hacer que los miembros sean filas de `workers`, cierra
`R-16` sin guarda adicional: el índice único ya entregado pasa a cubrir la unión.

La historia recoge además el resto de defectos de lo entregado que la segunda pasada encontró y que
no encajan en ninguna historia existente (`R-15`, `R-17`, `R-18` documental, `R-20`, `R-21`), con el
mismo criterio con el que `MVP-207` cerró los de la primera pasada.

## Objetivo

Dejar la épica cerrable: que exista **una sola identidad de responsable** utilizable por la operativa
diaria, que ningún maestro admita dos filas indistinguibles —tampoco a través de la frontera
miembro/cuadrilla—, que la administración de invitaciones sea simétrica en los dos canales y que el
contrato publicado de los cuatro maestros describa los errores que la API devuelve de verdad.

## Requisitos de usuario

### HU-1 — Poder elegir a cualquier persona como responsable de una labor

**Como** usuario que va a registrar actividad en el diario,
**quiero** elegir como responsable tanto a un miembro del Workspace como a alguien de la cuadrilla
sin cuenta, desde una sola lista,
**para** que el registro se guarde sin que yo tenga que saber de qué «tipo» es cada persona.

### HU-2 — No tener dos personas con el mismo nombre en el maestro

**Como** usuario que mantiene el maestro de trabajadores,
**quiero** que el sistema me impida dar de alta a alguien de la cuadrilla con el nombre de un miembro
del Workspace,
**para** no acabar con dos «Andrés Gilabert» en el desplegable de responsables.

### HU-3 — Retirar una invitación por enlace que se me ha ido de las manos

**Como** miembro del Workspace que ha compartido un enlace de invitación,
**quiero** poder anularlo desde la aplicación,
**para** que deje de servir en cuanto sepa que ha llegado a quien no debía.

### HU-4 — Que la aplicación no me diga que no tengo temporadas cuando sí las tengo

**Como** usuario que ha cerrado la campaña y todavía no ha abierto la siguiente,
**quiero** que la pantalla que me ofrece crear una temporada reconozca las que ya tengo y me deje
activar una,
**para** no crear una temporada duplicada ni chocar con un error de nombre repetido.

### HU-5 — Confiar en el contrato de los maestros

**Como** persona que implementa la operativa diaria (MVP-003/MVP-004),
**quiero** que el contrato publicado de terrenos, temporadas, trabajadores y tareas indique los
códigos de error que devuelve cada ruta,
**para** no programar un manejo de errores que nunca se dispara.

## Alcance (in-scope)

### Identidad del responsable (P-034 · CA-3 de la épica)

- **Materializar una fila de `workers` por cada miembro del Workspace**, vinculada a su cuenta por el
  `user_account_id` ya reservado en `MVP-204` (opción (a) recomendada en `P-034`). El maestro de
  trabajadores pasa a ser **el** maestro de responsables: miembros y cuadrilla sin cuenta comparten
  un único espacio de identificadores.
- **Un solo listado de responsables**: `GET /api/v1/workers` devuelve las dos clases de persona con
  una señal que las distingue, y la UI deja de combinar dos endpoints en cliente.
- **Coherencia automática con la membresía**, sin acción manual:
  - Al crear un Workspace y al aceptarse una invitación, la persona aparece como responsable.
  - Al revocarse el acceso (`MVP-204`, CA-7), deja de ser seleccionable sin invalidar los registros
    que ya la referencian.
  - El nombre de un responsable con cuenta **no se edita en el maestro**: llega de la identidad de
    Google (RN-036). Sí es editable su tarifa horaria, que es dato operativo.
- **Migración de los datos existentes**, con la misma política de `MVP-207`: se materializan los
  miembros actuales y, si un trabajador de cuadrilla ya ocupaba el nombre de un miembro, el nombre lo
  conserva **el miembro** (no es renombrable) y la fila de cuadrilla recibe el sufijo « (2)».
- `ACTIVITY.worker_id` **no cambia**: sigue siendo una FK simple a `workers`, y el contrato de
  actividades no se reabre. Es el motivo por el que se elige esta opción y no un responsable
  polimórfico.

### Correcciones de lo entregado

- **Anulación de invitaciones de canal `enlace`** (`R-15`): la acción existe en la API desde
  `MVP-207` y el contrato la promete «de cualquier canal», pero no hay ninguna pantalla desde la que
  ejecutarla. Se expone en la UI.
- **Superficie única de invitaciones pendientes** (`R-21`): decidir y dejar una sola superficie de
  administración, con las mismas acciones para los dos canales, en vez de las dos actuales
  («Invitaciones pendientes» de `/app/invitations`, que lista enlaces y no tiene acciones, y
  «Miembros y accesos», que tiene acciones y no lista enlaces).
- **Oferta de temporada honesta** (`R-17`): en un Workspace con temporadas pero ninguna activa, la
  pantalla deja de afirmar que no hay ninguna y ofrece **activar una existente** además de crear una
  nueva. Incluye la píldora de cabecera, que hoy solo ofrece crear.
- **Contrato de los cuatro maestros** (`R-18`, parte documental): las secciones Plots, Seasons,
  Workers y Tasks de `contratos-api.md` distinguen los códigos de error del **alta** de los de la
  **edición**, que hoy no coinciden.
- **Checklist del Home** (`R-20`): el paso «Trabajadores» deja de marcarse pendiente en un Workspace
  que, por RN-027, ya tiene responsables seleccionables.
- **Detalle de «Miembros y accesos»**: una persona en estado `invitado` muestra hoy su email dos
  veces (`name ?? email` y de nuevo `email`). Se corrige de paso, por tocarse la pantalla.

## Fuera de alcance (out-of-scope)

- **La entidad `ACTIVITY`** y el registro de actividades: siguen siendo `MVP-301`. Aquí solo se deja
  el responsable direccionable y se cierra `P-028` desde el lado del maestro.
- **Unificación de los códigos de validación en el borde de transporte**: el
  `InvalidModelStateResponseFactory` colapsa toda la validación de alta a `VALIDATION_REQUIRED` y
  filtra un mensaje genérico en inglés. Es transversal a toda la API y se resuelve con el arnés de
  integración: `MVP-999`, `P-043` (con `P-027`). Aquí solo se corrige la **documentación** para que
  describa lo que la API hace hoy.
- **Unicidad de nombre entre Workspaces del mismo usuario**: `MVP-999`, `P-044`.
- **Vocabulario de estado de una temporada desbancada** («planificada» para una campaña pasada):
  `MVP-999`, `P-045`.
- **Pantalla de error para rutas desconocidas**: `MVP-999`, `P-046`.
- **Convertir un trabajador de cuadrilla en miembro** (vincular una fila existente a una cuenta): la
  materialización es en un solo sentido, de miembro a `workers`. Sigue en `MVP-999` (`P-022`).
- Campos de rol/especialidad y teléfono en trabajadores (`P-035`), borrado y fusión de registros de
  maestro (`P-036`/`P-041`), aviso a la persona invitada de que su invitación se anuló (`P-039`).
- Normalización avanzada de nombres (acentos, similitud): sigue fuera, igual que en `MVP-205` y
  `MVP-207`.

## Criterios de aceptación

- [x] **CA-1**: Todo miembro activo del Workspace tiene una fila propia en `workers` vinculada a su
  cuenta, de modo que cualquier responsable seleccionable —miembro o cuadrilla— se identifica con un
  `workers.id` y puede guardarse en `ACTIVITY.worker_id` sin campos alternativos ni texto libre.
- [x] **CA-2**: `GET /api/v1/workers` devuelve el maestro completo de responsables con una señal que
  distingue a quien tiene cuenta de la cuadrilla sin ella, y la pantalla de Trabajadores se construye
  desde ese único listado.
- [x] **CA-3**: Dentro de un mismo Workspace no se puede crear ni renombrar un trabajador de cuadrilla
  con el nombre de un miembro, ignorando mayúsculas; el intento responde `409
  CONFLICT_WORKER_NAME_DUPLICATE` y la invariante la garantiza en base de datos el índice
  `ux_workers_workspace_name` ya existente.
- [x] **CA-4**: El maestro sigue a la membresía sin intervención manual: al aceptarse una invitación
  la persona aparece como responsable, al revocarse su acceso deja de ser seleccionable sin invalidar
  los registros que la referencian, y su nombre no se puede editar ni borrar desde el maestro
  (RN-036).
- [x] **CA-5**: La migración materializa a los miembros existentes sin perder datos: si un trabajador
  de cuadrilla ocupaba el nombre de un miembro, el miembro conserva el nombre y la fila de cuadrilla
  se renombra con sufijo, con el mismo criterio que `MVP-207`.
- [x] **CA-6**: Una invitación pendiente de canal `enlace` se puede anular desde la aplicación; tras
  anularla, el enlace deja de permitir la aceptación y la invitación desaparece de la lista de
  pendientes.
- [x] **CA-7**: Las invitaciones pendientes se administran desde una sola superficie, con las mismas
  acciones disponibles para los dos canales; no quedan dos listas del mismo concepto con reglas
  distintas.
- [x] **CA-8**: En un Workspace con temporadas pero ninguna activa, la pantalla de oferta no afirma
  que no haya ninguna y permite **activar** una existente además de crear una nueva; la píldora de
  cabecera conduce a la misma decisión.
- [x] **CA-9**: Las secciones Plots, Seasons, Workers y Tasks de `contratos-api.md` describen los
  códigos de error que devuelve realmente cada ruta, distinguiendo el alta de la edición.
- [x] **CA-10**: El bloque de preparación del Home no marca como pendiente un maestro que RN-027 ya
  da por poblado, y ningún texto de la pantalla contradice el estado real.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/TrabajadoresView.tsx](../../../../../prototype/terrenario-mvp/src/components/TrabajadoresView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/TemporadasView.tsx](../../../../../prototype/terrenario-mvp/src/components/TemporadasView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| TrabajadoresView | RN-027, RN-036 | cubierto | UI conducida: la pantalla se pinta desde `GET /workers` (miembros con `MIEMBRO`/«Editar tarifa»/«Gestionar acceso» y cuadrilla con «Inactivar»/«Editar»); alta con el nombre de un miembro → 409 mostrado en el modal sin perder lo tecleado; `PATCH` de nombre o `is_active` sobre un miembro → 422 (CA-1/CA-2/CA-3/CA-4) |
| ActivityModal | RN-002, RN-027 | cubierto | `GET /workers` devuelve un único espacio de identificadores: `meta:{total, members, crew}` y `kind` por fila, con `ux_workers_workspace_user_account` garantizando una fila por cuenta (CA-1) |
| Miembros y accesos (sin prototipo) | RN-034, RN-035 | cubierto | La lista incluye la invitación de canal `enlace` con «Generar enlace nuevo» y «Anular enlace»; anularla la retira y el preview del enlace pasa a `anulada/reason:"cancelled"`; `/app/invitations` deja de duplicar la lista (CA-6/CA-7) |
| TemporadasView · oferta de temporada | RN-021, RN-022 | cubierto | Workspace con dos temporadas y ninguna activa: «tiene 2 temporadas, pero ninguna activa», botón «Activar» por temporada, nombre sugerido libre («Campaña 2027») y píldora «Sin temporada activa · Elegir» (CA-8) |
| Home del área operativa (sin prototipo) | RN-021, RN-027 | cubierto | Tras la materialización, el paso «Trabajadores» aparece **hecho** con su recuento y su ayuda deja de contradecirlo (CA-10) |

## Notas y decisiones

- **Origen y trazabilidad.** Cierra los hallazgos `R-15`, `R-16`, `R-17`, `R-18` (parte documental),
  `R-20` y `R-21` del registro de triage de [MVP-299](../MVP-299--revision-epica/spec.md), y el punto
  `P-034` de `MVP-999`. Al materializar `user_account_id` cierra también `P-022` en su parte de
  «miembro → trabajador», y `P-028` desde el lado del maestro.
- **Decisión del PO (2026-07-28): la identidad del responsable se resuelve aquí, no en `MVP-301`.**
  El motivo es no arrastrar la incidencia: `P-034` estaba marcado como bloqueante y el CA-3 de la
  épica lo afirma como cumplido. Cerrarlo dentro de `MVP-002` permite marcar ese CA con evidencia en
  vez de reformularlo, y evita que `MVP-003` construya el diario sobre un responsable que no se puede
  guardar.
- **Por qué la opción (a) y no un responsable polimórfico.** Materializar la fila de `workers`
  mantiene un único espacio de identificadores para el diario (`MVP-301`) y el dashboard (`MVP-004`),
  no reabre el contrato de actividades y hace que la guarda de duplicados ya entregada por `MVP-207`
  cubra la unión sin código nuevo. La opción (b) (`worker_id?` XOR `member_user_id?`) obligaría a
  duplicar la lógica en cada consumidor.
- **Qué queda del `GET /workspace-members`.** Sigue siendo la superficie de **accesos** (estado de
  membresía, revocar, invitar, reenviar, anular). Lo que cambia es que deja de ser también la fuente
  de responsables: eso pasa a `GET /workers`. Las dos vistas siguen existiendo con propósitos
  distintos y el enlace entre ellas se mantiene.
- **Punto a cerrar en el `tech-design`.** Qué ocurre cuando el nombre de display de Google cambia
  después de materializar la fila: resincronizar y, si el nuevo nombre colisiona con otra fila del
  maestro, aplicar el sufijo a la fila de cuadrilla (el miembro no es renombrable). Debe quedar
  decidido antes de implementar.
- **Un miembro no se puede inactivar a mano en el maestro**, porque RN-027 obliga a que todo miembro
  sea seleccionable. Su disponibilidad la gobierna la membresía: revocar el acceso es la vía de
  retirarlo. Si el PO quisiera después «miembros que no hacen labor», sería una historia propia.
- **Riesgo de la historia: alto.** Toca el modelo de un maestro ya entregado y con datos, con
  migración de backfill y renombrado. La política de datos preexistentes es la misma que el PO ya
  aprobó en `MVP-207` (conservar y renombrar, nunca borrar ni hacer fallar la migración: la API migra
  al arrancar).
- **Puntos nuevos abiertos por esta revisión**, en `MVP-999`: `P-043` (códigos de validación del alta
  y mensaje en inglés, con `P-027`), `P-044` (Workspaces homónimos del mismo usuario), `P-045`
  (temporada desbancada rotulada «planificada», con `P-021`) y `P-046` (sin pantalla de 404 bajo
  `/app`, con `P-025`/`P-037`).
- **Cierre de la épica.** `MVP-002` no cierra hasta entregar esta historia y hacer la tercera pasada
  de verificación en `MVP-299`, donde se marcarán los CA-1, CA-3 y CA-4 de la épica.
