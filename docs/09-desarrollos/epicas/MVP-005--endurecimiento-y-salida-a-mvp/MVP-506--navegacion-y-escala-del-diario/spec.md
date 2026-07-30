---
id: "MVP-506"
tipo: feature
titulo: "Navegación y escala del diario: paginación, búsqueda en servidor y filtro por responsable"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "4d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-301", "MVP-303", "MVP-304", "MVP-305"]
bloquea: []
relacionado_con: ["MVP-399"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["diario", "escala", "rendimiento"]
  modulo_path: "03-modulos/"
  componentes: ["diario", "paginacion", "busqueda", "filtros"]
  etiquetas: ["mvp", "diario", "escala", "hardening"]
  nivel_riesgo: medio
creado_en: "2026-07-30"
actualizado_en: "2026-07-30"
---

# MVP-506 — Navegación y escala del diario: paginación, búsqueda en servidor y filtro por responsable

## Contexto

El **diario cronológico unificado** (`GET /api/v1/diary`, MVP-305) es la **vista principal del MVP**
(RN-033), la que más se abre. Hoy devuelve **todas** las entradas vivas del Workspace y el cliente las
pinta enteras: en un Workspace recién estrenado son decenas, pero una explotación con dos campañas de
histórico llega a **miles**. La revisión de `MVP-004` (`MVP-499`) confirma que el resto de la producción
es fiel al contrato; esta historia cierra la **escala y la navegación** del diario antes de la salida a
MVP, dentro del endurecimiento de `MVP-005`.

Reúne tres puntos de `MVP-999` que comparten la misma pasada (todos tocan `GET /diary`):

- **`P-051`** — el diario **no pagina**. Las convenciones de `contratos-api.md` ya definen el patrón
  (`?page=&limit=` con `meta:{ total, page, limit }`); falta aplicarlo. **Aviso clave**: el diario
  unificado mezcla los tres tipos **en memoria** en el servidor (reutilizando los puertos de actividad,
  compra y consumo). Es equivalente **mientras no haya paginación**, pero deja de serlo en cuanto la
  haya: *paginar sobre tres listas ya materializadas no es paginar*. Quien resuelva esto debe **mover la
  mezcla a SQL** en la misma pasada, no solo añadir `page`/`limit`.
- **`P-052`** — la **búsqueda por texto** del diario sigue siendo **local** sobre lo ya filtrado
  (terreno, temporada y tipo sí viajan al servidor desde MVP-305). Es coherente hoy —teclear no dispara
  una petición por letra—, pero deja de serlo con paginación: *buscar sobre una página no es buscar*.
- **`P-056`** — el diario **no deja filtrar por responsable** (`worker_id`), aunque el dato existe en la
  actividad y `GET /activities` ya lo soporta. «Qué hizo Antonio esta semana» no se puede responder
  desde la vista principal.

## Objetivo

Que el diario siga siendo usable y correcto cuando el histórico crece: paginado de verdad (con la mezcla
en SQL), con búsqueda y filtros resueltos en servidor, para que ninguna operación mienta al aplicarse
solo sobre una página.

## Requisitos de usuario

### HU-1 — Un diario que no se degrada con el histórico

**Como** usuario de una explotación con varias campañas,
**quiero** que el diario cargue por páginas y no de golpe,
**para** que la vista principal siga siendo ágil cuando hay miles de registros.

### HU-2 — Buscar y filtrar sobre todo el diario, no sobre lo que se ve

**Como** usuario que busca una entrada concreta,
**quiero** que la búsqueda por texto y el filtro por responsable se apliquen sobre el diario completo,
**para** encontrar lo que busco aunque esté fuera de la página actual.

## Alcance (in-scope)

- **Paginación en servidor** de `GET /api/v1/diary` con el patrón de las convenciones de la KB
  (`?page=&limit=`, `meta:{ total, page, limit }`).
- **Mezcla de los tres tipos en SQL** (UNION/consulta paginada), reemplazando la mezcla en memoria de
  MVP-305, condición para que la paginación sea real.
- **Búsqueda por texto en servidor** (P-052), sustituyendo la búsqueda local del cliente.
- **Filtro por responsable** (`worker_id`) en `GET /diary` (P-056), coherente con `GET /activities`.
- Ajuste del cliente (`DiarioView`) para consumir la paginación y mover búsqueda/responsable al servidor.

## Fuera de alcance (out-of-scope)

- Rediseño visual del diario o de sus tarjetas.
- Exportación del diario o informes.
- Paginación de los listados de cada recurso por separado (`/activities`, `/harvests`, …): esta historia
  es del **diario unificado**; si algún listado la necesita, se decide aparte.

## Criterios de aceptación

- [ ] **CA-1**: `GET /api/v1/diary` pagina en servidor (`page`/`limit` + `meta:{ total, page, limit }`) y
  la mezcla de los tres tipos se resuelve **en SQL**, de modo que la paginación es real (no un recorte de
  tres listas ya materializadas en memoria).
- [ ] **CA-2**: La búsqueda por texto del diario se resuelve en servidor y el filtro por **responsable**
  (`worker_id`) está disponible; ambos operan sobre el diario completo, no sobre la página visible.
- [ ] **CA-3**: `DiarioView` consume la paginación y delega búsqueda y filtro por responsable al
  servidor, sin regresión de los filtros ya existentes (terreno, temporada, tipo).

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> La fuente de verdad funcional es la KB. El patrón de paginación es el de las convenciones de
> `contratos-api.md`.

## Notas y decisiones

- **Decisión abierta (a tomar en la historia): patrón de paginación.** Opciones registradas en `P-051`:
  paginación clásica (`page`/`limit`), scroll infinito, o ventana temporal por defecto (p. ej. la
  temporada de trabajo). Defecto propuesto: **paginación clásica**, porque las convenciones de
  `contratos-api.md` ya la definen y es la de menor riesgo; el scroll infinito es una capa de cliente que
  puede añadirse encima sin cambiar el contrato.
- **Origen.** Consolida `P-051`, `P-052` y `P-056` de `MVP-999`, detectados en `MVP-301`/`MVP-305`/
  `MVP-399`. Se ubica en `MVP-005` (endurecimiento) porque el núcleo del punto es **escala/salida a MVP**,
  no funcionalidad nueva del diario, y así no reabre `MVP-003` (Hito C, ya cerrado). Decisión del PO
  (2026-07-30).
- **`P-052` está parcialmente resuelto** desde MVP-305 (terreno/temporada/tipo ya en servidor); aquí se
  cierra su parte pendiente (la búsqueda por texto).
