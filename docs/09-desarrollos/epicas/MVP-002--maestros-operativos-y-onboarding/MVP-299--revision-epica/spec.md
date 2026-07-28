---
id: "MVP-299"
tipo: feature
titulo: "Revision epica"
estado: en-progreso
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

## Fuera de alcance (out-of-scope)

- Implementar en esta historia los nuevos cambios detectados.
- Redefinir objetivos de negocio ya aprobados para la epica.
- Sustituir actividades de QA o validacion tecnica de historias previas.

## Criterios de aceptación

- [ ] **CA-1**: Existe una revision funcional final de la epica con evidencias de lo validado.
- [ ] **CA-2**: Todos los puntos ciegos o requisitos pendientes detectados quedan documentados.
- [ ] **CA-3**: Cada punto pendiente se transforma en una nueva historia en la epica correspondiente o en MVP-999 cuando aplique.

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

## Conformidad de lo entregado (2026-07-28)

| Historia | CA | Veredicto | Evidencia principal |
|---|---|---|---|
| MVP-201 | 3/3 | conforme | Workspace nuevo redirige a la oferta cancelable; pildora de temporada en cabecera; indice unico `ux_seasons_workspace_active` |
| MVP-202 | 3/3 | conforme | Alta con solo `name`+`ownership_type` a `201`; `PATCH` parcial; `is_active` reversible; aviso de `tree_count` ausente (RN-010) |
| MVP-203 | 3/3 | conforme | Crear una segunda temporada desbanca a la activa (pasa a `planificada`); cerrar y reabrir; `end_date` anterior a `start_date` a `400` |
| MVP-204 | 8/8 | conforme | `GET /workspace-members` con vista unificada `activo`+`invitado`, `can_revoke`/`is_self`; reenvio por email y por enlace. CA-7/CA-8 solo por suite de tests (entorno de un unico usuario) |
| MVP-205 | 3/3 | conforme | Catalogo vacio por Workspace; alta y renombrado en linea; duplicado por mayusculas a `409 CONFLICT_TASK_NAME_DUPLICATE` |
| MVP-206 | 10/10 | conforme | Renombrado, `mode: only_delete`, baja logica, perdida de contexto y `reopen` verificados end-to-end. CA-6 (email a los miembros) solo por test unitario: el entorno no tiene un segundo miembro |
| MVP-207 | 4/6 conformes, 2 con hueco | ver detalle | Verificada en la 2a pasada (2026-07-28). CA-2/CA-3/CA-5/CA-6 conformes; CA-1 conforme con salvedad (`R-18`) y CA-4 incompleto (`R-15`) |

Las siete historias estan funcionalmente entregadas. En `MVP-207` quedan dos huecos sobre lo que sus
propios criterios prometen:

- **CA-1 (contrato de temporadas), conforme con salvedad**: la seccion §2 de `contratos-api.md` ya
  describe rutas, opcionalidad, estado inicial y errores reales de la ruta de **edicion**, pero la de
  **alta** sigue mal descrita. Hallazgo `R-18`.
- **CA-4 (anular invitacion pendiente), incompleto**: correcto para el canal `email`, no alcanzable
  para el canal `enlace`, que es el unico caso sin destinatario y el de mayor riesgo. Hallazgo
  `R-15`.

**Correccion de la 1a pasada sobre los CA de la epica.** La primera pasada afirmo que «los CA-2 y
CA-3 de la epica se cumplen». Es incorrecto para el **CA-3**: la operativa no puede depender
exclusivamente de estos maestros porque el **responsable** no es direccionable (`R-07`/`P-034`).
Estado real de los criterios de la epica al cerrar la 2a pasada:

| CA de la epica | Veredicto | Motivo |
|---|---|---|
| CA-1 | pendiente | Solo por el estado de esta historia y de `MVP-208`; es mecanico |
| CA-2 | cumple | Un Workspace nuevo arranca con temporada y maestros minimos sin configuracion tecnica |
| CA-3 | **no cumple** | Terreno, temporada y tarea si; **responsable no**: un miembro elegido como responsable no se puede guardar (`P-034`). Se resuelve en `MVP-208` por decision del PO |
| CA-4 | **parcial** | Sin duplicados dentro de cada maestro, pero si en la union miembro/cuadrilla (`R-16`); contrato fiel salvo la ruta de alta (`R-18`) |
| CA-5 | cumple | Ciclo de vida del Workspace cerrado y verificado end-to-end en la 1a pasada |

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
| R-07 | 2026-07-28 | MVP-002 / MVP-204 | tecnico/modelo | No hay identidad unica de «responsable» para MVP-301. `ACTIVITY.worker_id` es FK a `workers` y el contrato exige `worker_id*`, pero por decision de MVP-204 (P-022) los miembros no son filas de `workers`: se exponen desde `workspace_members` con `user_id`. RN-027 obliga a que todo miembro sea seleccionable como responsable, asi que hoy no hay forma de guardarlo ni endpoint unificado de responsables. P-028 cubre la *tarea* de ACTIVITY, no el *responsable* | alto | si | aprobado-crear-historia | Registrado como P-034 en MVP-999, inicialmente con destino MVP-301 y recomendacion de materializar fila `workers` por miembro via `user_account_id`. **Reasignado en la 2a pasada (decision del PO, 2026-07-28): se resuelve dentro de MVP-002, en la historia MVP-208 (CA-1), para no arrastrar la incidencia y poder marcar el CA-3 de la epica con evidencia** |
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

## Notas y decisiones

- Esta historia debe ejecutarse siempre como cierre de la epica.
- No se marca la epica como completada hasta cerrar esta historia.
- La implementacion de los puntos detectados es alcance de `MVP-207` y `MVP-208`, no de esta historia
  (que es de gobierno de alcance). Los CA de `MVP-299` se marcaran en la tercera pasada, cuando
  `MVP-208` este entregada y verificada.
- **Leccion de la 2a pasada**: una revision de cierre debe verificar tambien los **criterios de la
  epica**, no solo los de cada historia. Las siete historias estaban conformes una a una y aun asi el
  CA-3 de la epica no se cumplia, porque el hueco vivia justo en la costura entre `MVP-204` y la
  epica que lo consume. El mismo patron produjo `R-16` (guarda por tabla frente a maestro definido
  como union) y `R-17` (estado alcanzable solo cruzando `MVP-201` con `MVP-203`).
