---
id: "MVP-207"
tipo: feature
titulo: "Correcciones de cierre de la épica de maestros"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-201", "MVP-202", "MVP-203", "MVP-204", "MVP-205"]
bloquea: []
relacionado_con: ["MVP-299", "MVP-206"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["terrenos", "temporadas", "trabajadores", "workspaces", "calidad"]
  modulo_path: "03-modulos/"
  componentes: ["terrenos", "temporadas", "trabajadores", "workspace-members", "app-shell"]
  etiquetas: ["mvp", "masters", "correccion", "contratos"]
  nivel_riesgo: medio
creado_en: "2026-07-28"
actualizado_en: "2026-07-28"
---

# MVP-207 — Correcciones de cierre de la épica de maestros

## Contexto

La revisión de cierre de la épica (`MVP-299`) verificó las seis historias entregadas contra la API
real y la UI conducida. Las seis cumplen sus criterios de aceptación, pero la revisión detectó cinco
defectos de lo ya entregado que no encajan en ninguna historia existente y que conviene resolver
**dentro de MVP-002**, antes de que MVP-003 y MVP-004 empiecen a generar histórico sobre estos
maestros:

- El **contrato publicado de temporadas** (`contratos-api.md`) describe una API que no es la
  entregada: MVP-201 y MVP-203 fueron las únicas historias de la épica que no actualizaron su
  sección. MVP-003 va a consumir ese contrato.
- La **guarda de duplicados de nombre existe solo en el catálogo de tareas**. En temporadas estaba
  contratada en la KB (`CONFLICT_SEASON_NAME_DUPLICATE`) y no se implementó; en trabajadores y
  terrenos nunca se planteó. Verificado en la API: se crearon tres trabajadores «Juan Perez» /
  «juan perez», dos terrenos «Prueba» y dos temporadas «2025/2026», todos con `201` e
  indistinguibles en pantalla.
- No se puede **anular una invitación pendiente**: «Miembros y accesos» reenvía y revoca, pero no
  retira a una persona en estado `invitado`.
- **Terrenos** es el único maestro que queda detrás de la guarda de oferta de temporada.
- El **Home** no conduce a los maestros y su copy contradice el estado real del menú lateral.

Son correcciones de lo ya entregado en la épica, por lo que se resuelven dentro de MVP-002, con el
mismo criterio con el que `MVP-106` cerró los defectos detectados en `MVP-199`.

## Objetivo

Dejar los maestros de la épica coherentes entre sí y con su contrato publicado: un nombre no se
puede repetir dentro de un mismo Workspace, el contrato de temporadas describe exactamente la API
entregada, la administración de personas es simétrica (invitar, reenviar, anular, revocar) y todos
los maestros se comportan igual respecto de la temporada y del arranque de la aplicación.

## Requisitos de usuario

### HU-1 — No poder duplicar un nombre en un maestro

**Como** usuario que mantiene los maestros del Workspace,
**quiero** que el sistema me impida crear dos temporadas, dos trabajadores o dos terrenos con el
mismo nombre,
**para** no acabar con filas indistinguibles que después no sé a cuál corresponde un registro.

### HU-2 — Anular una invitación que ya no quiero

**Como** miembro del Workspace que ha invitado a la persona equivocada,
**quiero** poder anular una invitación pendiente,
**para** que ese enlace deje de servir y esa persona no acabe entrando en mi explotación.

### HU-3 — Confiar en el contrato de la API de temporadas

**Como** persona que implementa la operativa diaria (MVP-003),
**quiero** que el contrato publicado de temporadas describa la API realmente entregada,
**para** no construir sobre campos, estados y códigos de error que no existen.

### HU-4 — Entrar a los maestros sin tropiezos

**Como** usuario que acaba de crear un Workspace,
**quiero** llegar a cualquier maestro desde el primer momento y que la aplicación me diga qué hacer
a continuación,
**para** poder preparar la explotación sin perderme ni encontrarme desvíos inesperados.

## Alcance (in-scope)

- **Reconciliación del contrato de temporadas** (`docs/02-arquitectura/contratos-api.md`, § Seasons)
  con lo entregado por MVP-201/MVP-203: `end_date` opcional; la temporada creada nace **activa**
  (decisión «crear cambia la activa», P-017); `PATCH` acepta `is_closed?` y no `status?`; el listado
  no admite filtros; documentar `GET /api/v1/seasons/active` y `POST /api/v1/seasons/{id}/activate`;
  sustituir los códigos de error inexistentes (`VALIDATION_DATE_RANGE_INVALID`,
  `CONFLICT_SEASON_ACTIVE_DUPLICATE`) por los reales de `ErrorCodes`.
- **Guarda de nombre único por Workspace, ignorando mayúsculas, en `seasons`, `workers` y `plots`**,
  con el mismo patrón de dos niveles que MVP-205 (P-026): guarda de aplicación en el repositorio
  más **índice único** sobre `(workspace_id, lower(name))`, y traducción de la violación a `409`
  con código propio por recurso.
- **Anulación de una invitación pendiente**: endpoint de cancelación y acción en «Miembros y
  accesos». La invitación anulada deja de ser aceptable por su enlace y desaparece de la lista de
  personas del Workspace.
- **Coherencia de la guarda de oferta de temporada**: `/app/terrenos` pasa a comportarse como el
  resto de maestros de administración (ver decisión en Notas). Se incluye también `/app/invitations`,
  que producía el mismo desvío desde «Miembros y accesos» (`MVP-999`, P-038; decisión del PO de
  corregirlo aquí para no arrastrarlo).
- **Arranque de la aplicación**: el Home conduce a los maestros pendientes de poblar y su copy deja
  de anunciar como «por habilitar» módulos que ya están encendidos.

## Fuera de alcance (out-of-scope)

- **Identidad unificada del responsable de una actividad** (miembro del Workspace frente a fila de
  `workers`): es una decisión de modelo que consume MVP-301; registrada en `MVP-999` (P-034).
- Normalización avanzada de nombres (acentos, similitud fonética, espacios internos): la guarda es
  de igualdad ignorando mayúsculas, igual que en MVP-205.
- Borrado físico de registros de maestro creados por error: registrado en `MVP-999` (P-036).
- Campos del prototipo no portados en Trabajadores (rol/especialidad, teléfono): `MVP-999` (P-035).
- Indicación de sección activa y agrupación del menú lateral: `MVP-999` (P-037, con P-025).
- Rediseño del Home como dashboard: la Visión General es alcance de MVP-004.

## Criterios de aceptación

- [x] **CA-1**: La sección de temporadas de `contratos-api.md` describe exactamente la API
  entregada: rutas existentes (incluidas `GET /seasons/active` y `POST /seasons/{id}/activate`),
  obligatoriedad real de cada campo, estado con el que nace la temporada creada y códigos de error
  que existen en `ErrorCodes`.
- [x] **CA-2**: Dentro de un mismo Workspace no se pueden crear ni renombrar dos temporadas, dos
  trabajadores ni dos terrenos con el mismo nombre ignorando mayúsculas; el intento responde `409`
  con un código de error propio del recurso y la UI lo explica sin perder lo tecleado.
- [x] **CA-3**: La invariante de CA-2 está garantizada también en base de datos con un índice único
  sobre `(workspace_id, lower(name))`, de modo que la guarda de aplicación y los datos no puedan
  discrepar.
- [x] **CA-4**: Un miembro puede anular una invitación pendiente desde «Miembros y accesos»; tras
  anularla, el enlace deja de permitir la aceptación y la persona desaparece de la lista de
  personas del Workspace.
- [x] **CA-5**: Todos los maestros de administración se comportan igual respecto de la oferta de
  temporada: entrar a Terrenos en un Workspace sin temporada activa no produce un desvío que los
  demás maestros no producen. Se extiende a `/app/invitations` (P-038), que producía ese mismo desvío
  al pulsar «Invitar persona» desde «Miembros y accesos»: toda la administración queda fuera de la
  guarda y el Home es el único destino que la conserva.
- [x] **CA-6**: Tras el primer acceso, el Home ofrece un camino explícito a los maestros que faltan
  por poblar y ningún texto de la pantalla contradice los módulos realmente disponibles en el menú.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/TemporadasView.tsx](../../../../../prototype/terrenario-mvp/src/components/TemporadasView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/TrabajadoresView.tsx](../../../../../prototype/terrenario-mvp/src/components/TrabajadoresView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| TemporadasView | RN-022 | cubierto | 409 `CONFLICT_SEASON_NAME_DUPLICATE` en alta y renombrado (incluidas las cerradas) verificado contra la API real; sección de temporadas de `contratos-api.md` reescrita (CA-1/CA-2/CA-3) |
| TrabajadoresView | RN-027 | cubierto | 409 `CONFLICT_WORKER_NAME_DUPLICATE` verificado con «Juan Perez»/«juan PEREZ»; índice `ux_workers_workspace_name` creado (CA-2/CA-3) |
| TerrenosView | RN-028 | cubierto | 409 `CONFLICT_PLOT_NAME_DUPLICATE` con aviso en el modal sin perder lo tecleado; `/app/terrenos` carga sin temporada activa en UI conducida (CA-2/CA-3/CA-5) |
| Miembros y accesos (sin prototipo) | RN-034, RN-035 | cubierto | «Anular invitación» con confirmación: la persona desaparece de la lista y el enlace responde `viewer.reason: "cancelled"` / 422 al aceptar (CA-4) |
| Home del área operativa (sin prototipo) | RN-021 | cubierto | Bloque «Prepara tu explotación · 2/4» con CTA a cada maestro pendiente y copy alineado con el menú (CA-6) |

## Notas y decisiones

- **Origen y trazabilidad.** Cierra los hallazgos `R-05`, `R-06`, `R-08`, `R-09` y `R-10` del
  registro de triage de [MVP-299](../MVP-299--revision-epica/spec.md).
- **Ámbito de la guarda de duplicados.** Se aplica a **todo el maestro**, no solo a los registros
  activos, igual que `ux_tasks_workspace_name` en MVP-205: inactivar «Poda» no libera el nombre.
  Es la opción coherente con el motivo por el que se inactiva en vez de borrar (no romper el
  histórico que referencia ese nombre).
- **Datos preexistentes — decisión del PO (2026-07-28): renombrar.** El índice único no se puede
  crear sobre un maestro que ya contenga duplicados, así que la migración los resuelve antes:
  conserva intacto el registro más antiguo de cada grupo y renombra el resto con sufijo «&nbsp;(2)»,
  «&nbsp;(3)»… por orden de `created_at`. No se pierde nada y el usuario los renombra o inactiva después
  desde la UI. **Inactivarlos se descartó** porque no resuelve el problema (la guarda cubre todo el
  maestro, así que las filas inactivas siguen ocupando su nombre) y **hacer fallar la migración**
  también, porque la API migra al arrancar y dejaría sin levantar cualquier entorno con datos sucios.
  Que no queden filas fusionables se registra en `MVP-999` (P-041, con P-036).
- **Decisión del PO (2026-07-28) sobre CA-5: `/app/terrenos` sale de la guarda.** Se confirma la
  propuesta: Terrenos pasa junto al resto de maestros de administración. Es lo que ya afirma el
  comentario de `App.tsx` y lo coherente con MVP-203/204/205 (un maestro se administra aunque el
  Workspace no tenga temporada). La alternativa —meter todos los maestros dentro de la guarda— haría
  que preparar la explotación exigiera antes crear una temporada, en contra de la decisión de
  producto de MVP-201 de que la temporada sea un acto cancelable. **`/app/invitations` entra en la
  misma corrección** (P-038): no es un maestro, así que quedaba fuera de la letra de CA-5, pero
  producía exactamente el mismo desvío al pulsar «Invitar persona» desde «Miembros y accesos», que sí
  estaba fuera de la guarda. Decisión del PO: corregirlo aquí para no arrastrar el error. Tras el
  cambio, el **único** destino detrás de la guarda es el Home (`/app`): la oferta de temporada vuelve
  a estar donde MVP-201 la quería, al entrar y no al administrar.
- **Anulación frente a rechazo.** El rechazo (`POST /invitations/{token}/reject`, MVP-107) lo
  ejecuta la persona invitada; la anulación de esta historia la ejecuta el Workspace emisor. Son
  dos transiciones distintas sobre `workspace_invitations` y ambas dejan la invitación inservible.
- **Sin impacto en MVP-206.** Ninguna de estas correcciones toca el ciclo de vida del Workspace.
- **Deriva corregida de paso.** El catálogo cerrado `invitation_status` seguía declarando solo
  `pendiente, aceptada` pese a que MVP-107 añadió `rechazada`. Se corrige al añadir `anulada`, por
  ser la misma tabla y la misma clase de deriva que R-05; registrado en `MVP-999` (P-042).
- **Puntos nuevos abiertos por esta historia**, en `MVP-999`: P-038 (`/app/invitations` dentro de la
  guarda de temporada, **resuelto aquí**), P-039 (no se avisa a la persona invitada de que su
  invitación se anuló), P-040 (encaje del bloque de preparación del Home con la Visión General de
  MVP-004) y P-041 (los duplicados renombrados no se pueden fusionar).
