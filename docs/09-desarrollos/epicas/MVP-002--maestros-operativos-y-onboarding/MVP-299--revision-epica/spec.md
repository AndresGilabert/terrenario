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
relacionado_con: ["MVP-207"]
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

## Metodo de verificacion (2026-07-28)

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

## Conformidad de lo entregado (2026-07-28)

| Historia | CA | Veredicto | Evidencia principal |
|---|---|---|---|
| MVP-201 | 3/3 | conforme | Workspace nuevo redirige a la oferta cancelable; pildora de temporada en cabecera; indice unico `ux_seasons_workspace_active` |
| MVP-202 | 3/3 | conforme | Alta con solo `name`+`ownership_type` a `201`; `PATCH` parcial; `is_active` reversible; aviso de `tree_count` ausente (RN-010) |
| MVP-203 | 3/3 | conforme | Crear una segunda temporada desbanca a la activa (pasa a `planificada`); cerrar y reabrir; `end_date` anterior a `start_date` a `400` |
| MVP-204 | 8/8 | conforme | `GET /workspace-members` con vista unificada `activo`+`invitado`, `can_revoke`/`is_self`; reenvio por email y por enlace. CA-7/CA-8 solo por suite de tests (entorno de un unico usuario) |
| MVP-205 | 3/3 | conforme | Catalogo vacio por Workspace; alta y renombrado en linea; duplicado por mayusculas a `409 CONFLICT_TASK_NAME_DUPLICATE` |
| MVP-206 | 10/10 | conforme | Renombrado, `mode: only_delete`, baja logica, perdida de contexto y `reopen` verificados end-to-end. CA-6 (email a los miembros) solo por test unitario: el entorno no tiene un segundo miembro |

Las seis historias estan funcionalmente entregadas y conformes: ningun criterio de aceptacion
incumplido. Los CA-2 y CA-3 de la epica se cumplen; el CA-1 no, por el estado de gobernanza
recogido en R-01 y R-02.

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
| R-07 | 2026-07-28 | MVP-002 / MVP-204 | tecnico/modelo | No hay identidad unica de «responsable» para MVP-301. `ACTIVITY.worker_id` es FK a `workers` y el contrato exige `worker_id*`, pero por decision de MVP-204 (P-022) los miembros no son filas de `workers`: se exponen desde `workspace_members` con `user_id`. RN-027 obliga a que todo miembro sea seleccionable como responsable, asi que hoy no hay forma de guardarlo ni endpoint unificado de responsables. P-028 cubre la *tarea* de ACTIVITY, no el *responsable* | alto | si | aprobado-crear-historia | Decision de modelo que consume MVP-301. Registrado como P-034 en MVP-999 con destino MVP-301 y recomendacion de materializar fila `workers` por miembro via `user_account_id` |
| R-08 | 2026-07-28 | MVP-002 / MVP-204 | funcional/seguridad | No se puede anular una invitacion pendiente. «Miembros y accesos» reenvia y revoca a un activo, pero no retira a una persona en estado `invitado`: si se invita a un email equivocado, la invitacion sigue viva y aceptable hasta caducar. Rompe la simetria de CA-6/CA-7 de MVP-204 | medio | no | aprobado-crear-historia | Arreglar ya en la epica, en la superficie que entrego MVP-204. Historia MVP-207 (CA-4) |
| R-09 | 2026-07-28 | MVP-002 / MVP-202 | ux | Terrenos es el unico maestro detras de la guarda de oferta de temporada: en un Workspace sin temporada, `/app/terrenos` redirige a `/app/temporada/nueva` mientras `/temporadas`, `/trabajadores`, `/tareas`, `/miembros` y `/ajustes` cargan. El propio comentario de `App.tsx` afirma la regla que Terrenos incumple. Verificado end-to-end | medio | no | aprobado-crear-historia | Arreglar ya en la epica. Historia MVP-207 (CA-5), pendiente de confirmar la direccion con el PO |
| R-10 | 2026-07-28 | MVP-002 / MVP-201 | ux | El Home no conduce a los maestros y su copy quedo obsoleto: sigue diciendo que «los modulos de gestion (diario, terrenos, cosechas...) apareceran en el menu lateral a medida que se vayan habilitando» con seis entradas ya encendidas, y su unico CTA es «Invitar a alguien». HU-2 de MVP-201 pedia entrar a una aplicacion preparada para completar maestros basicos | medio | no | aprobado-crear-historia | Arreglar ya en la epica. Historia MVP-207 (CA-6) |
| R-11 | 2026-07-28 | MVP-002 / MVP-204 | ux/doc | Campos del prototipo no portados sin registrar la divergencia: `TrabajadoresView` del prototipo pide «Rol / Especialidad» y «Telefono» y el maestro real solo tiene `name` y `hourly_rate`. El checklist de MVP-204 marca la pantalla «cubierto» sin anotar la omision, a diferencia de MVP-202, que si registro las suyas en P-019 | bajo | no | aprobado-crear-historia | Diferido: decidir si el MVP quiere esos campos. Registrado como P-035 en MVP-999 |
| R-12 | 2026-07-28 | MVP-002 / MVP-202, MVP-204, MVP-205 | funcional | Un registro de maestro creado por error no se puede borrar, solo inactivar. RN-037 (borrado con confirmacion) cubre unicamente registros operativos, asi que un terreno, trabajador o tarea mal tecleado queda para siempre en la lista de inactivos | bajo | no | aprobado-crear-historia | Diferido. Registrado como P-036 en MVP-999 |
| R-13 | 2026-07-28 | MVP-002 / MVP-205 | ux | La navegacion lateral no marca la seccion activa (`AppSidebar` usa `button` mas `navigate`, sin `NavLink` ni `aria-current`). Con seis entradas ya encendidas se nota | bajo | no | aprobado-crear-historia | Diferido y consolidado con P-025 (agrupacion del menu). Registrado como P-037 en MVP-999 |
| R-14 | 2026-07-28 | MVP-002 / MVP-205 | doc/alcance | La accion pendiente de P-026 no se aplico: el spec de MVP-302 sigue listando «prevencion basica de duplicados evidentes» como alcance propio en vez de reutilizar la guarda ya entregada por MVP-205 | bajo | no | resuelto | Ajustado el alcance de MVP-302 en esta revision; P-026 queda cerrado del todo |

## Resultado de la revision (2026-07-28)

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

## Notas y decisiones

- Esta historia debe ejecutarse siempre como cierre de la epica.
- No se marca la epica como completada hasta cerrar esta historia.
- La implementacion de los puntos detectados es alcance de `MVP-207`, no de esta historia (que es de
  gobierno de alcance). Los CA de `MVP-299` se marcaran en la segunda pasada, cuando `MVP-207` este
  entregada y verificada.
