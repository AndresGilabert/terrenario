---
id: "MVP-299"
tipo: feature
titulo: "Revision epica"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-201", "MVP-202", "MVP-203", "MVP-204", "MVP-205", "MVP-206"]
bloquea: []
relacionado_con: ["MVP-207", "MVP-208"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["gobernanza", "calidad", "scope-control"]
  modulo_path: "03-modulos/"
  componentes: ["backlog", "qa", "stabilization"]
  etiquetas: ["mvp", "revision-epica", "cierre"]
  nivel_riesgo: medio
creado_en: "2026-07-24"
actualizado_en: "2026-07-28"
---

# MVP-299 — Revision epica

## Contexto

Durante la ejecucion de una epica aparecen ajustes, puntos ciegos y necesidades no previstas en las historias originales. Si no se centralizan antes del cierre, se dispersan y se pierde trazabilidad para decidir el trabajo posterior.

## Objetivo

Ejecutar una revision final de la epica para validar el funcionamiento global, consolidar los pendientes detectados y convertirlos en nuevas historias planificables.

## Requisitos de usuario

### HU-1 — Consolidar pendientes de la epica

**Como** Product Owner,
**quiero** reunir en un solo punto los ajustes y requisitos detectados durante la epica,
**para** evitar omisiones y cerrar el alcance con trazabilidad.

### HU-2 — Verificar calidad funcional final

**Como** equipo de producto y desarrollo,
**quiero** revisar el estado final de la epica sobre el flujo integrado,
**para** abrir nuevas historias concretas con evidencias de error o falta.

## Alcance (in-scope)

- Revision integral del comportamiento entregado por la epica.
- Consolidacion de puntos ciegos y requisitos pendientes detectados durante las historias previas.
- Creacion de nuevas historias para cubrir errores, faltas o ajustes detectados.
- Priorizacion inicial de los nuevos items segun impacto funcional y de negocio.
- **Correcciones menores de cierre** detectadas en la ultima pasada, cuando no justifican una historia
  propia. Ampliacion acordada con el PO en la 3a pasada (2026-07-28); ver «Notas y decisiones».
- **Retriage de los puntos transversales aparcados** en `MVP-999` y **revision previa de la epica
  siguiente**, para no arrastrar pendientes ni abrirla con contradicciones. Ampliacion acordada con el
  PO en la 3a pasada; ver «Preparacion de las epicas siguientes».

## Fuera de alcance (out-of-scope)

- Implementar en esta historia los cambios detectados que supongan **alcance nuevo** o que tengan
  entidad suficiente para una historia propia: esos siguen yendo a una historia de la epica
  (`MVP-207`, `MVP-208`) o a `MVP-999`.
- Redefinir objetivos de negocio ya aprobados para la epica.
- Sustituir actividades de QA o validacion tecnica de historias previas.

## Criterios de aceptación

- [x] **CA-1**: Existe una revision funcional final de la epica con evidencias de lo validado.
  _(Tres pasadas sobre el sistema real; ver «Metodo de verificacion» y la tabla de conformidad.)_
- [x] **CA-2**: Todos los puntos ciegos o requisitos pendientes detectados quedan documentados.
  _(23 hallazgos `R-01`..`R-28` en el registro de triage, con impacto, bloqueo y decision.)_
- [x] **CA-3**: Cada punto pendiente se transforma en una nueva historia en la epica correspondiente o
  en MVP-999 cuando aplique. _(`MVP-207` y `MVP-208` dentro de la epica; `P-034`..`P-050` en
  `MVP-999`; `R-24`/`R-25` corregidos aqui por ser menores.)_
- [x] **CA-4**: La rama de reasignacion de la baja de Workspace retira a quien sale de los responsables
  seleccionables: su fila de `workers` se inactiva sin borrarse, igual que al retirarle el acceso a
  mano, de modo que las dos vias que revocan una membresia dejan el maestro coherente (`R-25`).
- [x] **CA-5**: Las secciones Plots, Seasons, Tasks y Workers de `contratos-api.md` describen tambien
  el codigo real del **campo obligatorio en blanco** en el alta, que es el ultimo caso en el que la
  tabla contradecia a la API (`R-24`). Con ello el CA-4 de la epica queda cumplido.
- [x] **CA-6**: Los puntos pendientes de `MVP-999` estan revisados uno a uno: los que tienen dueño
  natural en una epica por delante quedan reasignados a ella, los que no cabian en ninguna historia
  existente tienen historia propia (`MVP-505`, `MVP-406`) y los que se quedan tienen motivo explicito.
- [x] **CA-7**: La epica siguiente (`MVP-003`) queda revisada antes de abrirse: sus contradicciones con
  la KB, el contrato y el ER estan resueltas o asignadas, y las decisiones de producto que
  bloqueaban su arranque (`G-1` borrado, `G-5` concurrencia) estan tomadas y documentadas.

## Maquetas y referencias visuales

- No aplica para esta historia de gobierno de alcance.

## Metodo de verificacion — 1a pasada (2026-07-28)

Revision realizada sobre el sistema real, no solo sobre build y tests:

- **Suite automatica**: `dotnet test` 290/290 en verde; `npm run build` y `npm run lint` sin errores
  nuevos; `validar_kb.py --validar` con 0 errores y 0 advertencias.
- **API real contra PostgreSQL** (`localhost:5127`, JWT de desarrollo): los cinco recursos con
  ambito de Workspace (`plots`, `seasons`, `workers`, `tasks`, `workspace-members`) y el ciclo de
  vida completo del Workspace (renombrar, `GET/POST /workspaces/active/closure`, baja logica, caida
  de contexto al Workspace por defecto, `reopen`), mas casos limite de validacion.
- **UI conducida** (`localhost:5173`): las seis pantallas entregadas por la epica y el onboarding
  completo sobre un Workspace creado expresamente para la prueba.
- Los datos de prueba creados durante la revision quedaron eliminados y el repositorio sin cambios.

## Metodo de verificacion — 2a pasada (2026-07-28, tras entregar MVP-207)

Segunda pasada sobre el sistema real, con el mismo criterio que la primera:

- **Suite y gates**: `dotnet test` 322/322 en verde; `npm run build` y `npm run lint` sin errores
  nuevos (solo los avisos preexistentes de `only-export-components`/`exhaustive-deps`);
  `validar_kb.py --validar` con 0 errores y 0 advertencias.
- **API real contra PostgreSQL** (`localhost:5127`, JWT de desarrollo): guarda de duplicados de los
  cuatro maestros en alta y en renombrado, ciclo completo de temporadas
  (crear -> desbancar -> cerrar -> `GET /seasons/active` a 404), anulacion de invitacion en los dos
  canales, aislamiento entre Workspaces, codigos de validacion de `POST` frente a `PATCH` y
  renombrado de Workspace.
- **Esquema**: verificados en PostgreSQL los cuatro indices
  `ux_{plots,seasons,workers,tasks}_workspace_name` y las columnas
  `cancelled_at`/`cancelled_by_user_id`; los cuatro repositorios traducen la violacion `23505` a
  `409`, de modo que una carrera no se escapa como 500.
- **UI conducida** (`localhost:5173`): Home, Terrenos, Temporadas, Trabajadores, Tareas, Miembros y
  accesos, Invitar y Ajustes, mas el comportamiento de la guarda de temporada en un Workspace sin
  temporada activa.
- Los datos de prueba creados quedaron eliminados y el repositorio sin cambios.

## Metodo de verificacion — 3a pasada (2026-07-28, tras entregar MVP-208)

Tercera pasada sobre el sistema real, con el mismo criterio que las dos anteriores:

- **Suite y gates**: `dotnet test` 350/350 en verde antes de tocar nada y 352/352 tras la correccion
  de `R-25`; `npm run build` sin errores; pipeline KB completo del CI
  (`validar_pipeline_kb.py --solo-cambios --check-indices-clean`, incluido markdownlint) con exit 0.
- **Esquema**: verificado en PostgreSQL el backfill de `MVP-208` —una fila de `workers` por miembro
  activo en los tres Workspaces, con `user_account_id` y el indice
  `ux_workers_workspace_user_account`—.
- **API real contra PostgreSQL** (`localhost:5127`, JWT de desarrollo): los diez CA de `MVP-208`; los
  codigos de error del contrato de los cuatro maestros **uno a uno**, comparando alta y edicion
  (nombre ausente, en blanco, demasiado largo, rangos, catalogos, 404, 409 y 422); guarda de
  duplicados cruzando la frontera miembro/cuadrilla con mayusculas y espacios sobrantes; ciclo
  completo de invitacion por enlace (crear -> reemitir con token rotado -> anular -> desaparece de
  pendientes); aislamiento entre Workspaces del maestro de responsables; y el arbol de decision de la
  baja de Workspace en su rama de reasignacion.
- **Reproduccion del defecto `R-25`**: sembrado un Workspace con dos propietarios y ejecutada la baja
  (`outcome: transferred`); comprobado en base de datos que la membresia quedaba `revocado` con la
  fila de `workers` todavia `is_active = true`. Tras la correccion, la misma ruta deja la fila
  inactiva, cubierta ademas por dos tests nuevos.
- **UI conducida** (`localhost:5173`): Trabajadores (listado unico, badge `MIEMBRO`, «Gestionar
  acceso», «Editar tarifa», filtro de inactivos), Miembros y accesos (invitacion por enlace con
  «Generar enlace nuevo» y «Anular enlace»), Invitar (sin lista duplicada), Home (checklist con
  «Trabajadores (n)» hecho), oferta de temporada con temporadas existentes y activacion desde la
  pantalla, pildora «Sin temporada activa · Elegir», Temporadas, Terrenos, Tareas y Ajustes. Sin
  errores de consola.
- Los datos de prueba creados quedaron eliminados y la base de datos restaurada a su estado previo.

## Conformidad de lo entregado (2026-07-28)

| Historia | CA | Veredicto | Evidencia principal |
|---|---|---|---|
| MVP-201 | 3/3 | conforme | Workspace nuevo redirige a la oferta cancelable; pildora de temporada en cabecera; indice unico `ux_seasons_workspace_active` |
| MVP-202 | 3/3 | conforme | Alta con solo `name`+`ownership_type` a `201`; `PATCH` parcial; `is_active` reversible; aviso de `tree_count` ausente (RN-010) |
| MVP-203 | 3/3 | conforme | Crear una segunda temporada desbanca a la activa (pasa a `planificada`); cerrar y reabrir; `end_date` anterior a `start_date` a `400` |
| MVP-204 | 8/8 | conforme | `GET /workspace-members` con vista unificada `activo`+`invitado`, `can_revoke`/`is_self`; reenvio por email y por enlace. CA-7/CA-8 solo por suite de tests (entorno de un unico usuario) |
| MVP-205 | 3/3 | conforme | Catalogo vacio por Workspace; alta y renombrado en linea; duplicado por mayusculas a `409 CONFLICT_TASK_NAME_DUPLICATE` |
| MVP-206 | 10/10 | conforme | Renombrado, `mode: only_delete`, baja logica, perdida de contexto y `reopen` verificados end-to-end. CA-6 (email a los miembros) solo por test unitario: el entorno no tiene un segundo miembro |
| MVP-207 | 6/6 | conforme | Verificada en la 2a pasada con dos huecos (`R-18`, `R-15`), ambos cerrados por `MVP-208` (CA-9 y CA-6). Ratificada en la 3a pasada |
| MVP-208 | 10/10 | conforme | Verificada en la 3a pasada (2026-07-28). CA-4 lo completa esta historia por la segunda via de revocacion (`R-25`) |

Las ocho historias estan funcionalmente entregadas y verificadas.

**Correccion de la 1a pasada sobre los CA de la epica.** La primera pasada afirmo que «los CA-2 y
CA-3 de la epica se cumplen». Era incorrecto para el **CA-3**: la operativa no podia depender
exclusivamente de estos maestros porque el **responsable** no era direccionable (`R-07`/`P-034`).
`MVP-208` lo resolvio. Estado de los criterios de la epica al cerrar la 3a pasada:

| CA de la epica | Veredicto | Evidencia |
|---|---|---|
| CA-1 | **cumple** | Las ocho historias en `completado` y esta historia cerrada en esta pasada; `_indice.md` regenerado a 9/9 |
| CA-2 | **cumple** | Un Workspace nuevo arranca con temporada y maestros minimos sin configuracion tecnica (1a pasada, ratificado) |
| CA-3 | **cumple** | Verificado en la 3a pasada: un miembro es direccionable por `workers.id` en un unico espacio de identificadores, asi que `ACTIVITY.worker_id` puede seguir siendo una FK simple para cualquier responsable (`P-034` resuelto en `MVP-208`) |
| CA-4 | **cumple** | Unicidad verificada en los cuatro maestros y **tambien** cruzando la frontera miembro/cuadrilla (`409 CONFLICT_WORKER_NAME_DUPLICATE` con mayusculas y espacios sobrantes). Contrato fiel en alta y edicion tras cerrar `R-18` en `MVP-208` (CA-9) y `R-24` aqui (CA-5) |
| CA-5 | **cumple** | Ciclo de vida verificado end-to-end en la 1a pasada; la incoherencia que la rama de reasignacion dejaba en el maestro (`R-25`) se corrige aqui (CA-4) |

## Registro de triage de la epica en curso

Usa esta seccion para decidir cuanto antes los puntos de alcance critico detectados dentro de
MVP-002, sin diferirlos a fases finales del MVP.

| Punto | Fecha deteccion | Origen (epica/historia) | Tipo | Descripcion breve | Impacto | Bloqueante | Estado de revision | Decision esperada |
|---|---|---|---|---|---|---|---|---|
| R-01 | 2026-07-28 | MVP-002 / todas | gobernanza | Ninguna de las historias entregadas figura en `completado`: MVP-201 y MVP-202 siguen `en-progreso` y MVP-203..206 en `borrador`, todas fusionadas y con sus CA cumplidos. `_indice.md` marca 0/7. Mismo patron que R-G en MVP-199 | alto | si | resuelto | Reconciliado en esta revision: MVP-201..206 pasan a `completado` y se regenera `_indice.md` |
| R-02 | 2026-07-28 | MVP-002 / MVP-206 | gobernanza | Los diez CA de MVP-206 estan sin marcar pese a estar implementados, con su checklist de prototipo en «cubierto» y P-004 cerrado como resuelto en MVP-999 | alto | si | resuelto | Reconciliado en esta revision: CA-1..CA-10 marcados con su evidencia |
| R-03 | 2026-07-28 | MVP-002 / MVP-206 | gobernanza | El spec de la epica no se actualizo al absorber MVP-206: su objetivo y alcance solo hablan de maestros y su fuera-de-alcance menciona «gestion avanzada de ownership». Una historia de 8 d y riesgo alto entro sin rastro en el alcance de la epica | medio | no | resuelto | Documentada la absorcion en el spec de la epica, como hizo MVP-001 con P-013/P-015/P-016 |
| R-04 | 2026-07-28 | MVP-002 / MVP-206, MVP-204 | gobernanza | Fechas de front-matter obsoletas: `reglas-de-negocio.md` marca `actualizado_en: 2026-07-20` habiendo anadido RN-038/039/040 el 28-07; el spec de la epica marca `2026-07-24` habiendo cambiado en el commit de MVP-204 | bajo | no | resuelto | Corregidas en esta revision |
| R-05 | 2026-07-28 | MVP-002 / MVP-201, MVP-203 | tecnico/doc | La seccion de temporadas de `contratos-api.md` describe una API que no es la entregada: `end_date*` obligatorio (es opcional), `201 { status: "planificada" }` (nace `activa`), `PATCH ... status?` (es `is_closed?`), filtros `status?`/`include_closed?` inexistentes, y tres codigos de error que no existen en `ErrorCodes`. Faltan `GET /seasons/active` y `POST /seasons/{id}/activate`. MVP-201 y MVP-203 fueron las unicas historias de la epica que no tocaron el contrato; MVP-003 lo va a consumir | alto | si | aprobado-crear-historia | Arreglar ya en la epica. Historia MVP-207 (CA-1) |
| R-06 | 2026-07-28 | MVP-002 / MVP-202, MVP-203, MVP-204 | funcional | La guarda de duplicados existe solo en el catalogo de tareas. En temporadas estaba **contratada** en la KB (`CONFLICT_SEASON_NAME_DUPLICATE`) y no se implemento; en trabajadores y terrenos nunca se planteo, pese a que HU-1 de MVP-204 dice literalmente que el maestro existe «para evitar nombres duplicados o inconsistentes». Verificado en la API: tres trabajadores «Juan Perez»/«juan perez», dos terrenos «Prueba» y dos temporadas «2025/2026», todos `201` e indistinguibles en pantalla | alto | si | aprobado-crear-historia | Arreglar ya en la epica, antes de que MVP-003/004 generen historico: anadir el indice unico despues obligaria a una migracion con limpieza de datos (mismo criterio que P-026). Historia MVP-207 (CA-2/CA-3) |
| R-07 | 2026-07-28 | MVP-002 / MVP-204 | tecnico/modelo | No hay identidad unica de «responsable» para MVP-301. `ACTIVITY.worker_id` es FK a `workers` y el contrato exige `worker_id*`, pero por decision de MVP-204 (P-022) los miembros no son filas de `workers`: se exponen desde `workspace_members` con `user_id`. RN-027 obliga a que todo miembro sea seleccionable como responsable, asi que hoy no hay forma de guardarlo ni endpoint unificado de responsables. P-028 cubre la _tarea_ de ACTIVITY, no el _responsable_ | alto | si | aprobado-crear-historia | Registrado como P-034 en MVP-999, inicialmente con destino MVP-301 y recomendacion de materializar fila `workers` por miembro via `user_account_id`. **Reasignado en la 2a pasada (decision del PO, 2026-07-28): se resuelve dentro de MVP-002, en la historia MVP-208 (CA-1), para no arrastrar la incidencia y poder marcar el CA-3 de la epica con evidencia** |
| R-08 | 2026-07-28 | MVP-002 / MVP-204 | funcional/seguridad | No se puede anular una invitacion pendiente. «Miembros y accesos» reenvia y revoca a un activo, pero no retira a una persona en estado `invitado`: si se invita a un email equivocado, la invitacion sigue viva y aceptable hasta caducar. Rompe la simetria de CA-6/CA-7 de MVP-204 | medio | no | aprobado-crear-historia | Arreglar ya en la epica, en la superficie que entrego MVP-204. Historia MVP-207 (CA-4) |
| R-09 | 2026-07-28 | MVP-002 / MVP-202 | ux | Terrenos es el unico maestro detras de la guarda de oferta de temporada: en un Workspace sin temporada, `/app/terrenos` redirige a `/app/temporada/nueva` mientras `/temporadas`, `/trabajadores`, `/tareas`, `/miembros` y `/ajustes` cargan. El propio comentario de `App.tsx` afirma la regla que Terrenos incumple. Verificado end-to-end | medio | no | aprobado-crear-historia | Arreglar ya en la epica. Historia MVP-207 (CA-5), pendiente de confirmar la direccion con el PO |
| R-10 | 2026-07-28 | MVP-002 / MVP-201 | ux | El Home no conduce a los maestros y su copy quedo obsoleto: sigue diciendo que «los modulos de gestion (diario, terrenos, cosechas...) apareceran en el menu lateral a medida que se vayan habilitando» con seis entradas ya encendidas, y su unico CTA es «Invitar a alguien». HU-2 de MVP-201 pedia entrar a una aplicacion preparada para completar maestros basicos | medio | no | aprobado-crear-historia | Arreglar ya en la epica. Historia MVP-207 (CA-6) |
| R-11 | 2026-07-28 | MVP-002 / MVP-204 | ux/doc | Campos del prototipo no portados sin registrar la divergencia: `TrabajadoresView` del prototipo pide «Rol / Especialidad» y «Telefono» y el maestro real solo tiene `name` y `hourly_rate`. El checklist de MVP-204 marca la pantalla «cubierto» sin anotar la omision, a diferencia de MVP-202, que si registro las suyas en P-019 | bajo | no | aprobado-crear-historia | Diferido: decidir si el MVP quiere esos campos. Registrado como P-035 en MVP-999 |
| R-12 | 2026-07-28 | MVP-002 / MVP-202, MVP-204, MVP-205 | funcional | Un registro de maestro creado por error no se puede borrar, solo inactivar. RN-037 (borrado con confirmacion) cubre unicamente registros operativos, asi que un terreno, trabajador o tarea mal tecleado queda para siempre en la lista de inactivos | bajo | no | aprobado-crear-historia | Diferido. Registrado como P-036 en MVP-999 |
| R-13 | 2026-07-28 | MVP-002 / MVP-205 | ux | La navegacion lateral no marca la seccion activa (`AppSidebar` usa `button` mas `navigate`, sin `NavLink` ni `aria-current`). Con seis entradas ya encendidas se nota | bajo | no | aprobado-crear-historia | Diferido y consolidado con P-025 (agrupacion del menu). Registrado como P-037 en MVP-999 |
| R-14 | 2026-07-28 | MVP-002 / MVP-205 | doc/alcance | La accion pendiente de P-026 no se aplico: el spec de MVP-302 sigue listando «prevencion basica de duplicados evidentes» como alcance propio en vez de reutilizar la guarda ya entregada por MVP-205 | bajo | no | resuelto | Ajustado el alcance de MVP-302 en esta revision; P-026 queda cerrado del todo |
| R-15 | 2026-07-28 (2a pasada) | MVP-002 / MVP-207 | funcional/seguridad | Una invitacion de canal `enlace` no se puede anular desde ninguna pantalla. El backend lo soporta y el contrato lo promete «de cualquier canal»; «Miembros y accesos» solo proyecta invitaciones `email` (el enlace no tiene destinatario) y la unica pantalla que si las lista, «Invitaciones pendientes» de `/app/invitations`, es de solo lectura. Verificado: enlace creado y anulado solo por API (204 -> preview `cancelled` -> accept 422); en UI no hay control. El caso de mayor riesgo (enlace anonimo filtrado) es el unico que no se puede retirar, y vive 7 dias | medio | no | aprobado-crear-historia | Arreglar ya en la epica: es el HU-2/CA-4 de MVP-207 sin completar. Historia MVP-208 (CA-6). **Resuelto en MVP-208**: «Miembros y accesos» lista tambien las invitaciones de canal `enlace` y las anula desde la UI (verificado: 204, preview `anulada/reason:"cancelled"` y desaparicion de la lista) |
| R-16 | 2026-07-28 (2a pasada) | MVP-002 / MVP-204, MVP-207 | funcional | La guarda de nombre unico de MVP-207 es **por tabla**, no sobre la union que RN-027 define como maestro de responsables. Verificado: con el miembro «Andres Gilabert» presente, `POST /workers` con ese mismo nombre responde `201` y `/app/trabajadores` muestra dos personas indistinguibles, una en «Miembros del Workspace» y otra en «Cuadrilla sin cuenta». Es el motivo literal de HU-1 de MVP-204 y lo que R-06 pretendia cerrar | medio | no | aprobado-crear-historia | Decision del PO (2026-07-28): no arrastrar la incidencia. Se resuelve con P-034 en MVP-208 (CA-3): al materializar los miembros como filas de `workers`, el indice ya entregado cubre la union sin codigo nuevo. **Resuelto en MVP-208 (CA-3)**: verificado que `POST /workers` con el nombre de un miembro —en mayusculas y con espacios sobrantes— responde `409 CONFLICT_WORKER_NAME_DUPLICATE`, sin guarda adicional |
| R-17 | 2026-07-28 (2a pasada) | MVP-002 / MVP-201, MVP-203 | ux/funcional | La oferta de temporada miente cuando el Workspace tiene temporadas pero ninguna activa. Tras MVP-203 ese estado es alcanzable (cerrar la activa libera el hueco, comportamiento contratado): `/app` desvia a `/app/temporada/nueva`, que dice «Crea tu primera temporada» y «X aun no tiene temporada». Verificado con un Workspace con una temporada `planificada` y otra `cerrada`. Ademas solo ofrece **crear**, no activar una existente, y reescribir el mismo nombre choca con el 409 nuevo de MVP-207. La pildora de cabecera («Sin temporada · Crear») tiene el mismo sesgo | medio | no | aprobado-crear-historia | Arreglar ya en la epica: es el cruce de dos historias suyas. Historia MVP-208 (CA-8). **Resuelto en MVP-208**: la oferta reconoce las temporadas existentes, ofrece **activar** una y sugiere un nombre libre al crear; la pildora pasa a «Sin temporada activa · Elegir» |
| R-18 | 2026-07-28 (2a pasada) | MVP-002 / MVP-202, MVP-203, MVP-204, MVP-205, MVP-207 | tecnico/doc | Los codigos de error del **alta** de los cuatro maestros no son los que publica el contrato. `InvalidModelStateResponseFactory` (`Program.cs`) colapsa toda la validacion de binding a `VALIDATION_REQUIRED`: un nombre demasiado largo en `POST` de `plots`/`workers`/`seasons`/`tasks` devuelve `VALIDATION_REQUIRED` con mensaje «…es demasiado largo», mientras que el mismo caso en `PATCH` si devuelve `VALIDATION_*_NAME_LENGTH`. Un `start_date` mal formado en `POST /seasons` devuelve ademas el mensaje generico **en ingles** «The request field is required.», que la UI muestra tal cual. Misma clase de deriva que R-05, corregida por MVP-207 solo en temporadas y solo en la mitad de la ruta. Afecta al CA-4 de la epica | medio | no | aprobado-crear-historia | Partido: la correccion **documental** de las cuatro secciones, en MVP-208 (CA-9), que es lo que cierra el CA-4 de la epica; la **unificacion de codigos** en el borde de transporte y el mensaje en ingles, a MVP-999 como P-043 (con P-027), por ser transversal a toda la API. **Parte documental resuelta en MVP-208 (CA-9)**: las cuatro secciones separan los codigos del alta de los de la edicion, con un aviso comun que explica por que difieren y remite a P-043 |
| R-19 | 2026-07-28 (2a pasada) | MVP-002 / MVP-206, MVP-102 | funcional/ux | Dos Workspaces del mismo usuario pueden llamarse igual. Verificado: renombrar «Test 02» a «Test 01» responde `200` y `GET /workspaces` devuelve dos entradas identicas; el selector (MVP-104) y el dialogo de baja (MVP-206) las muestran indistinguibles. La epica cerro la unicidad «en los maestros», pero el Workspace —el contenedor cuyo ciclo de vida absorbio— quedo fuera de MVP-102, MVP-206 y MVP-207. El riesgo real es dar de baja el Workspace equivocado | bajo | no | aprobado-crear-historia | Diferido: no es un maestro y el impacto es de confusion, no de integridad. Registrado como P-044 en MVP-999 |
| R-20 | 2026-07-28 (2a pasada) | MVP-002 / MVP-207 | ux | El checklist del Home contradice su propio texto: calcula el paso «Trabajadores» con `GET /workers` (solo cuadrilla sin cuenta) mientras su propia ayuda dice «los miembros del Workspace ya cuentan». Como todo Workspace tiene al menos un miembro y por RN-027 ya hay un responsable seleccionable, el paso aparece pendiente igualmente | bajo | no | aprobado-crear-historia | Arreglar en la epica, de paso con R-17 y con la materializacion de miembros de MVP-208, que ademas lo hace consistente por construccion. Historia MVP-208 (CA-10). **Resuelto en MVP-208**: `GET /workers` devuelve ya el maestro completo, asi que el paso aparece hecho con su recuento y su ayuda deja de contradecirlo |
| R-21 | 2026-07-28 (2a pasada) | MVP-002 / MVP-204, MVP-207 | ux/alcance | Dos superficies asimetricas para el mismo concepto: «Invitaciones pendientes» de `/app/invitations` (MVP-103) **incluye** los enlaces y **no** tiene acciones; la lista de personas de «Miembros y accesos» (MVP-204) **excluye** los enlaces y **si** las tiene. Ninguna historia decide cual es la superficie canonica, y el solape es lo que deja a R-15 sin sitio donde resolverse | bajo | no | aprobado-crear-historia | Se decide al resolver R-15, en la misma pasada. Historia MVP-208 (CA-7). **Resuelto en MVP-208**: la superficie canonica es «Miembros y accesos» —la que ya tenia las acciones y la lista unificada de MVP-204—, extendida a los dos canales con las mismas acciones (renovar y anular); `/app/invitations` se queda con el alta y un acceso a la administracion |
| R-22 | 2026-07-28 (2a pasada) | MVP-002 / MVP-203 | ux | Una temporada desbancada se etiqueta «PLANIFICADA» aunque sea pasada. Verificado: crear «2026/2027» desbanca a «2025/2026», que pasa a `planificada` y asi se rotula en el maestro. Es coherente con la derivacion de estados y esta contratado, pero de cara al usuario una campaña ya vivida rotulada «Planificada» engaña, y `season_status` es un catalogo cerrado de producto | bajo | no | aprobado-crear-historia | Diferido: el vocabulario lo decidira quien filtre por temporada. Registrado como P-045 en MVP-999, a consolidar con P-021 y revisar al cierre de MVP-004 |
| R-23 | 2026-07-28 (2a pasada) | MVP-002 / MVP-205, MVP-207 | ux/tecnico | No hay pantalla de error para rutas desconocidas: `App.tsx` mapea `/app/*` a `HomeView` y el resto a `/`, asi que un enlace roto o un error de tecleo renderiza el Home sin informar de nada. Ninguna historia lo cubre | bajo | no | aprobado-crear-historia | Diferido y consolidado con la deuda de navegacion. Registrado como P-046 en MVP-999, con P-025 y P-037 |
| R-24 | 2026-07-28 (3a pasada) | MVP-002 / MVP-202, MVP-203, MVP-204, MVP-205, MVP-208 | tecnico/doc | El contrato del **alta** sigue prometiendo codigos que la API no devuelve en cinco filas: «nombre en blanco» en terrenos, temporadas, tareas y trabajadores, y «`ownership_type` en blanco» en terrenos, anunciados como `VALIDATION_REQUIRED_*` cuando la API responde `VALIDATION_REQUIRED`. La causa es la misma que el propio aviso de la seccion ya explica —`[Required]` no admite cadenas vacias, asi que el valor no llega al dominio—, de modo que **la prosa de la seccion contradecia a su propia tabla**. `MVP-208` (CA-9) separo alta y edicion pero no reviso estas filas. Verificado contra la API en las cuatro secciones, y verificado tambien que en `PATCH` si sale el codigo especifico. Afecta al CA-4 de la epica | bajo | no | resuelto | Correccion **documental** menor (cinco celdas y un matiz en el aviso), sin historia propia: se aplica en esta misma revision (CA-5). Decision del PO (2026-07-28). La unificacion tecnica de los codigos del alta sigue en `MVP-999` (`P-043`, con `P-027`) |
| R-25 | 2026-07-28 (3a pasada) | MVP-002 / MVP-206, MVP-208 | funcional | **La baja de Workspace con copropietarios revoca el acceso pero no retira al responsable.** Hay dos puntos que revocan una membresia y solo uno mantiene el maestro alineado: `RevokeMemberHandler` llama a `MemberRosterService.SuspendMemberAsync` y `CloseWorkspaceHandler.ReassignAsync` no. Verificado end-to-end sembrando un Workspace con dos propietarios: tras la baja (`outcome: transferred`) la membresia queda `revocado` y su fila de `workers` sigue `is_active = true`. Contradice el CA-4 de `MVP-208`, la seccion §4 de `contratos-api.md` («sale al revocarse su acceso, que **inactiva** su fila») y el `modelo-de-datos.md`. Impacto hoy: quien hereda el Workspace ve al que se fue listado como «MIEMBRO» activo en Trabajadores mientras «Miembros y accesos» lo da por revocado. Impacto en MVP-301: apareceria en el desplegable de responsables de un Workspace ajeno. Es el mismo patron de costura entre historias que produjo `R-16` y `R-17`: ni `MVP-206` ni `MVP-208` lo ven desde dentro | medio | no | resuelto | Correccion menor (una llamada y su transaccion ya compartida), sin historia propia: se aplica en esta misma revision (CA-4), con dos tests de regresion —la rama que reasigna inactiva la fila, la que da de baja no toca el maestro—. Decision del PO (2026-07-28) |
| R-26 | 2026-07-28 (3a pasada) | MVP-002 / MVP-204, MVP-206 | funcional/alcance | **Un miembro no propietario no puede abandonar un Workspace.** `MVP-204` (CA-7) cubre retirar el acceso **a otro** y la UI oculta la accion sobre uno mismo (`is_self`); `MVP-206` cubre la salida **del propietario** (traspaso o baja). Nadie cubre la salida voluntaria de un miembro corriente: arrastra ese Workspace en su selector para siempre y —desde `MVP-208`— sigue siendo responsable seleccionable en el. Es el hueco simetrico de `MVP-206`, y no figura en ninguna historia de MVP-001..MVP-006 ni como punto de `MVP-999` | medio | no | aprobado-crear-historia | Diferido: no bloquea a MVP-003/MVP-004 ni corrompe datos, y la epica ya se amplio dos veces. Registrado como P-048 en MVP-999 |
| R-27 | 2026-07-28 (3a pasada) | MVP-002 / MVP-204, MVP-206 | ux/coherencia | La UI nunca ofrece revocar a un copropietario aunque la API lo permita: `can_revoke` se calcula como `activo && rol != owner`, mientras la guarda real de `RevokeMemberHandler` solo protege al propietario **unico** (`CountActiveOwnersAsync <= 1`), que es lo que dice el CA-8 de `MVP-204`. Con varios propietarios —escenario que abrio `MVP-206`— la regla publicada y la accion disponible no coinciden. No es un fallo de seguridad: la UI es mas restrictiva que la API | bajo | no | aprobado-crear-historia | Diferido: misma superficie y misma decision de producto que R-26. Registrado como P-049 en MVP-999, con P-048 |
| R-28 | 2026-07-28 (3a pasada) | MVP-002 / MVP-203 | doc | El ER de `PURCHASE` no declara `season_id` ni la relacion `SEASON -> PURCHASE`, pese a que RN-021 exige que **toda compra** quede asociada a una temporada y a que `contratos-api.md` §7 la contrata como `season_id*`. Misma clase de deriva que `P-028` (la tarea de `ACTIVITY`), detectada al comprobar que el CA-3 de la epica se cumple tambien para compras. Nace en la costura entre el maestro de temporadas de esta epica y la operativa que lo consumira | bajo | no | aprobado-crear-historia | Diferido: la entidad esta pendiente y su dueño natural es quien la materialice. Registrado como P-050 en MVP-999 con destino MVP-303, junto a P-028 |

## Resultado de la revision — 1a pasada (2026-07-28)

Revision realizada con el PO sobre el flujo integrado de la epica. Los hallazgos se clasifican asi:

- **Arreglar ya, dentro de MVP-002 — historia `MVP-207`** (correcciones de lo entregado): R-05
  (contrato de temporadas), R-06 (guarda de duplicados en temporadas, trabajadores y terrenos),
  R-08 (anular invitacion pendiente), R-09 (coherencia de acceso a Terrenos) y R-10 (arranque de la
  aplicacion).
- **Decidir aqui, implementar en MVP-003**: R-07, la identidad del responsable de una actividad.
  Nace en esta epica y bloquea MVP-301, asi que se registra como **P-034** en `MVP-999` con destino
  `MVP-301` y una recomendacion explicita de modelo.
- **Diferido a `MVP-999`**: R-11 (campos de prototipo en Trabajadores, P-035), R-12 (borrado de
  registros de maestro creados por error, P-036) y R-13 (seccion activa del menu, P-037, a
  consolidar con P-025).
- **Housekeeping de esta revision**: R-01 y R-02 (estados de historia y CA de MVP-206), R-03
  (absorcion de MVP-206 en el alcance de la epica), R-04 (fechas de front-matter) y R-14 (alcance
  de MVP-302), todos aplicados aqui.

**Reconciliacion de gobernanza aplicada:**

- `MVP-201`, `MVP-202`, `MVP-203`, `MVP-204`, `MVP-205`, `MVP-206`: pasan a `completado`.
- `MVP-206`: checklist de CA cerrado (`CA-1..CA-10` marcados con su evidencia).
- Spec de la epica: documentada la absorcion de MVP-206 y anadida `MVP-207` a `historias`.
- `_indice.md` regenerado.

**Ya contemplado en otras historias (sin fila nueva de trabajo):** aviso de fecha fuera de rango
(P-017-d, MVP-003), produccion por temporada en el maestro (P-021, MVP-004), detalle de terreno con
historico (P-019), seleccion de tarea y responsable al registrar actividad (MVP-301),
`task_id`/`task_text` en ACTIVITY (P-028), arnes de tests de frontend (P-012/P-023, MVP-501), 500
ante `PATCH` con UTF-8 invalido (P-027), aviso in-app de reactivacion (P-029), plantillas de email
(P-030 con P-001), agrupacion del menu (P-025), perfil de usuario (P-032), retencion y expurgo
(P-033), baja de cuenta (P-024) y consolidacion de `03-modulos` (P-020).

Con la decision del PO, el alcance de la epica MVP-002 se amplia con `MVP-207`; la epica no se
cierra hasta entregarla y hacer una segunda pasada de verificacion en esta misma historia.

## Resultado de la revision — 2a pasada (2026-07-28)

Segunda pasada tras entregar `MVP-207`. `MVP-207` esta entregada y verificada, con dos huecos sobre
sus propios criterios (`R-15`, `R-18`). La pasada confirma ademas que el **CA-3 de la epica no se
cumple**: el responsable de una actividad no es direccionable, cosa que la 1a pasada dio por buena.
Los hallazgos nuevos se clasifican asi:

- **Arreglar ya, dentro de MVP-002 — historia `MVP-208`**: `R-16` junto con `P-034` (identidad unica
  del responsable, que cierra el CA-3 y el CA-4 de la epica), `R-15` (anular una invitacion de canal
  `enlace`), `R-21` (superficie unica de invitaciones pendientes), `R-17` (oferta de temporada con
  temporadas existentes), `R-18` en su parte documental (contrato de los cuatro maestros) y `R-20`
  (checklist del Home).
- **Decision del PO (2026-07-28) sobre `P-034`: no arrastrar la incidencia.** El punto estaba
  registrado con destino `MVP-301` y marcado como bloqueante. Se reasigna a `MVP-208`, dentro de esta
  epica: es el maestro de responsables el que esta incompleto, no el diario que lo consume, y
  resolverlo aqui permite marcar el CA-3 de la epica con evidencia en vez de reformularlo. Como
  efecto colateral cierra `R-16` sin codigo nuevo, porque el indice unico de `MVP-207` pasa a cubrir
  la union miembro/cuadrilla.
- **Diferido a `MVP-999`**: `R-18` en su parte tecnica (codigos de validacion del alta y mensaje en
  ingles, **P-043**, con `P-027`), `R-19` (Workspaces homonimos del mismo usuario, **P-044**), `R-22`
  (temporada desbancada rotulada «planificada», **P-045**, con `P-021`) y `R-23` (sin pantalla de 404
  bajo `/app`, **P-046**, con `P-025`/`P-037`).
- **Housekeeping de esta pasada**: corregida la afirmacion de la 1a pasada sobre el CA-3 de la epica
  y anadida la tabla de estado real de los cinco criterios; `P-034` reasignado; `P-022` marcado como
  cerrado por `MVP-208` en su parte de «miembro -> trabajador».

Con la decision del PO, el alcance de la epica se amplia con `MVP-208`; la epica no cierra hasta
entregarla y hacer una **tercera pasada** de verificacion en esta misma historia, donde se marcaran
los CA-1, CA-3 y CA-4 de la epica y los CA de `MVP-299`.

## Resultado de la revision — 3a pasada (2026-07-28)

Tercera pasada tras entregar `MVP-208`. **Las ocho historias estan entregadas y conformes**, y los
diez criterios de `MVP-208` se verificaron uno a uno contra la API real y la UI conducida. El
**CA-3 de la epica, que la 2a pasada dio por incumplido, se cumple**: un miembro es direccionable por
`workers.id` y la operativa diaria podra guardarlo sin campos alternativos. Los hallazgos nuevos se
clasifican asi:

- **Corregido aqui, por ser menor y no justificar historia propia** (decision del PO, 2026-07-28):
  `R-25` (la baja con copropietarios revoca el acceso sin retirar al responsable) y `R-24` (cinco
  filas del contrato del alta que la API no cumple). Son, respectivamente, la segunda via de
  revocacion que faltaba al CA-4 de `MVP-208` y las cinco celdas que faltaban a su CA-9: correcciones
  de lo ya entregado, no alcance nuevo. Pasan a ser los **CA-4 y CA-5 de esta historia**, con
  criterios verificables propios, para que no queden como housekeeping sin evidencia.
- **Diferido a `MVP-999`**: `R-26` (un miembro no propietario no puede abandonar un Workspace,
  **P-048**), `R-27` (`can_revoke` frente a la guarda real con varios propietarios, **P-049**, con
  `P-048`) y `R-28` (el ER de `PURCHASE` sin `season_id`, **P-050**, con destino `MVP-303` junto a
  `P-028`).
- **Housekeeping de esta pasada**: marcados los cinco CA de la epica con su evidencia, `MVP-299`
  pasa a `completado`, el spec de la epica pasa a `completado` y se regenera `_indice.md` (9/9).

**Por que `R-26` no amplia la epica una tercera vez.** El criterio de las dos ampliaciones anteriores
fue «no arrastrar la incidencia» cuando el hueco **bloqueaba** un criterio de la epica o la historia
siguiente: `MVP-206` sostenia a todos los maestros y `MVP-208` cerraba el CA-3. `R-26` no cumple
ninguna de las dos condiciones: no rompe ningun CA de la epica, no bloquea a `MVP-003`/`MVP-004` y no
corrompe datos. Es alcance nuevo del ciclo de vida de la **membresia**, no de los maestros.

## Preparacion de las epicas siguientes (3a pasada, 2026-07-28)

Antes de dar la epica por cerrada, el PO pidio aprovechar la pasada para dos cosas mas: **revisar los
puntos transversales aparcados** para el final del MVP y **revisar la epica siguiente** antes de
abrirla, de modo que no se arrastren pendientes ni contradicciones.

### Retriage de los 32 puntos pendientes de MVP-999

Detalle completo y criterio en la seccion «Retriage de la 3a pasada de MVP-299» de
[MVP-999](../../MVP-999--pendientes-transversales-y-diferidos/spec.md). Resumen:

- **16 puntos reasignados** a la epica que ya los consume: `P-012`/`P-023`/`P-031` a `MVP-501`,
  `P-027`/`P-043` a `MVP-502`, `P-021`/`P-045` a `MVP-403`/`MVP-405`, `P-036`/`P-040`/`P-041` a
  `MVP-499`, `P-019` (parte de detalle) a `MVP-004`, y `P-050` a `MVP-303`.
- **Dos historias nuevas**, para los dos grupos que no cabian en ninguna historia existente:
  - **`MVP-505` — Cumplimiento funcional de salida** (`P-008` paginas legales y consentimiento,
    `P-024` baja de cuenta, `P-033` retencion y expurgo). Estaban apuntando a `MVP-005` **sin encaje
    real**: `MVP-502` es hardening tecnico, `MVP-503` es revision documental con «nuevas politicas»
    fuera de alcance y `MVP-504` es el gate. `MVP-503` habria detectado el incumplimiento y `MVP-504`
    habria bloqueado la salida sin ninguna historia que lo resolviera. Los tres pasan a
    **bloqueantes**.
  - **`MVP-406` — Navegacion del area operativa** (`P-025` agrupacion del menu, `P-037` seccion
    activa, `P-046` ruta desconocida), en `MVP-004`: al cerrarla estan encendidos los diez modulos y
    el menu alcanza su tamaño definitivo, asi que se reestructura una sola vez.
- **6 puntos se quedan en `MVP-999`** con motivo explicito: emails (`P-001`/`P-030`/`P-039`),
  notificaciones in-app (`P-011`/`P-029`), abandonar Workspace (`P-048`/`P-049`), catalogo de modulos
  (`P-020`), mejoras diferibles (`P-022`/`P-032`/`P-035`) y homonimos (`P-044`/`P-047`).

### Revision previa de MVP-003, la epica siguiente

Revisadas sus cinco historias contra la KB, el contrato y el ER. Seis hallazgos, corregidos **antes**
de abrir la epica para no descubrirlos a mitad de construccion:

| Hallazgo | Tipo | Que pasaba | Resolucion |
|---|---|---|---|
| `G-1` | contradiccion | `RN-037` decia «borrado **fisico**» y el modelo de datos declara `deleted_at` y fija el borrado logico como convencion de las entidades operativas. `MVP-305` (CA-3) esta justo encima, y el contrato **no publicaba ninguna ruta `DELETE`** | **Decision del PO: gana el borrado logico.** `RN-037` reformulada, contrato con `DELETE` de baja logica y confirmacion explicita, alcance actualizado en `MVP-301`, `MVP-303`, `MVP-304`, `MVP-305` y `MVP-401` |
| `G-2` | hueco | El **consumo sin compra previa** —CA-3 de la epica y CA-2 de `MVP-304`, `RN-032`— no tenia donde vivir: `PURCHASE_CONSUMPTION.purchase_id` era FK obligatoria y la unica ruta contratada colgaba de una compra | `purchase_id` pasa a anulable, se contrata `POST /api/v1/consumptions` y el ER recoge los campos que faltaban. El **mecanismo** (columna anulable frente a entidad propia) lo cierra el `tech-design` de `MVP-304`; la revision fija los requisitos |
| `G-3` | hueco | El consumo solo tenia `created_at`: sin fecha de negocio ni temporada, pese a que el diario ordena cronologicamente (`RN-033`) y `RN-021` exige temporada | `date`, `season_id` y `product` añadidos al ER y al contrato; CA-4 nuevo en `MVP-304` |
| `G-4` | hueco entre epicas | `RN-033` define el diario como actividades + **cosechas** + compras/consumos, pero `MVP-305` lo construye sin cosechas y **ninguna historia de `MVP-004` mencionaba el diario**: se habria quedado incompleto para siempre | Encender la cosecha en el diario pasa a ser alcance de `MVP-401` (CA-4 nuevo), con nota en `MVP-305` para que la vista deje sitio al tercer tipo de entrada |
| `G-5` | huerfano | **`ADR-0005` (concurrencia optimista) no tenia historia.** Esta aceptado, el ER declara `version` y el contrato exige `If-Match` con `409 CONFLICT_VERSION_MISMATCH`, pero no habia una sola mencion a concurrencia en las seis epicas | **Decision del PO: se implementa en `MVP-003`**, con las entidades que lo estrenan, en vez de retrofitarlo en `MVP-005` sobre un cliente ya escrito sin manejo de conflicto. CA-4 nuevo en la epica, CA-4 en `MVP-301`, alcance en `MVP-303`/`MVP-304` y CA-5 en `MVP-401` |
| `G-6` | menor | `ACTIVITY.description` estaba contratado y no figuraba ni en el ER ni en el alcance de `MVP-301` | Añadido en ambos |

`MVP-004` y `MVP-006` se revisaron tambien: sin contradicciones propias mas alla de `G-4` y de los
puntos ya reasignados. `MVP-005` era coherente salvo por el hueco de cumplimiento que cubre `MVP-505`.

**Leccion**: los tres hallazgos serios de esta revision previa (`G-1`, `G-2`, `G-5`) son
**contradicciones entre documentos**, no huecos de redaccion de las historias: regla de negocio contra
modelo de datos, contrato contra ER, y ADR contra roadmap. Ninguno se ve leyendo una historia sola.
Conviene repetir este cruce —RN / ER / contrato / ADR / alcance de historias— al abrir cada epica, no
solo al cerrarla.

## Notas y decisiones

- Esta historia debe ejecutarse siempre como cierre de la epica.
- No se marca la epica como completada hasta cerrar esta historia.
- **Ampliacion del alcance de esta historia (decision del PO, 2026-07-28, 3a pasada).** Hasta la 2a
  pasada, `MVP-299` era gobierno de alcance puro: todo lo detectado se implementaba en una historia de
  la epica (`MVP-207`, `MVP-208`) o se difería. En la 3a pasada quedaban dos correcciones **menores**
  —una llamada de una linea y cinco celdas de una tabla—, ambas cierre de lo que `MVP-208` ya
  prometia. Abrir una novena historia para eso pesaba mas que el arreglo, asi que el PO decide
  hacerlas aqui. El limite queda acotado en el «Fuera de alcance»: **correcciones menores si, alcance
  nuevo no**; lo que tenga entidad de historia sigue yendo a una historia. Para compensar que la
  historia que verifica la epica pasa a verificar tambien codigo suyo, las dos correcciones llevan
  criterios de aceptacion propios (`CA-4`, `CA-5`) con su evidencia, en vez de quedar como
  housekeeping.
- **Leccion de la 2a pasada, confirmada en la 3a**: una revision de cierre debe verificar tambien los
  **criterios de la epica**, no solo los de cada historia. Las siete historias estaban conformes una a
  una y aun asi el CA-3 de la epica no se cumplia, porque el hueco vivia justo en la costura entre
  `MVP-204` y la epica que lo consume. El mismo patron produjo `R-16` (guarda por tabla frente a
  maestro definido como union), `R-17` (estado alcanzable solo cruzando `MVP-201` con `MVP-203`) y,
  en la 3a pasada, `R-25` (dos vias de revocacion y solo una mantiene el maestro) y `R-26` (dos
  historias cubren la salida del propietario y la de otro, ninguna la propia).
- **Leccion nueva de la 3a pasada**: verificar el contrato **ejecutando cada fila de su tabla** contra
  la API, no leyendola. `R-24` sobrevivio a la correccion de `R-18` porque esa correccion se hizo
  sobre el texto; las cinco filas que quedaron mal solo se ven ejecutandolas.
