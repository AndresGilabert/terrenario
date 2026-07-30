---
id: "MVP-209"
tipo: feature
titulo: "TDD: Estado de temporada y temporada de trabajo por usuario"
estado: completado
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["temporadas", "workspaces", "contexto-sesion"]
  modulo_path: "03-modulos/"
  componentes: ["temporadas", "workspace-members", "dashboard", "app-shell"]
  etiquetas: ["mvp", "temporadas", "modelo", "multiusuario"]
  nivel_riesgo: alto
creado_en: "2026-07-30"
actualizado_en: "2026-07-30"
---

# TDD: MVP-209 — Estado de temporada y temporada de trabajo por usuario

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Separa dos conceptos que el maestro de `MVP-203` fundía en `Season.is_active` (uno por Workspace):

- **Estado** de la temporada (informativo, derivado de fechas): `planificada` / `abierta` / `cerrada`.
- **Temporada de trabajo**: sobre cuál registra por defecto cada usuario, ahora **por usuario** en
  `workspace_members.active_season_id`.

Migración `SeasonWorkingPerMember` con backfill. Cierra `P-045`.

### Decisiones de diseño

- **El estado se deriva de `is_closed` + `start_date` frente a hoy**, sin mirar «activa». Como depende
  de «hoy», el agregado expone `StatusOn(DateOnly reference)` (no una propiedad): la fecha se pasa desde
  el borde (`DateOnly.FromDateTime(DateTime.UtcNow)`), para no acoplar el dominio al reloj y poder
  probarlo de forma determinista. `abierta` incluye las campañas **pasadas no cerradas**: siguen
  recibiendo registros tardíos (el rendimiento llega meses después). `cerrada` es el único estado que
  fija una acción explícita del usuario, y sigue siendo editable (RN-024).
- **La temporada de trabajo vive en `workspace_members.active_season_id`, no en `users`.** La de
  Workspace activo (`users.active_workspace_id`) es global por usuario; la de trabajo es **por
  Workspace**, y la membresía ya *es* el par (usuario, Workspace). FK `ON DELETE SET NULL`: borrar la
  temporada devuelve al usuario al defecto en vez de dejar una referencia colgada.
- **Se resuelve por petición, no viaja en el JWT.** A diferencia del Workspace activo —que sí es un
  claim de scope y reemite token al cambiar—, la temporada de trabajo es solo un defecto/preferencia y
  no gobierna el aislamiento, así que meterla en el token solo añadiría reemisiones. `ActivateSeason`
  y `CreateSeason` la fijan con un `UPDATE` directo a la membresía del usuario, que **no toca a nadie
  más** (CA-2).
- **Regla de defecto** (`WorkingSeasonPolicy`, dominio puro y testeable): sin nada fijado, se elige la
  campaña **abierta que contiene hoy**; si no, la abierta más reciente; si no, la más reciente; `null`
  si el Workspace no tiene temporadas. Se resuelve **al leer** (no se autoescribe en la membresía): la
  columna solo se rellena cuando el usuario elige o crea una temporada.
- **Riesgo acotado por la exploración previa**: ninguna **escritura** operativa resuelve «activa» en
  servidor —actividades, cosechas, compras y consumos reciben `season_id` del cliente y solo validan
  pertenencia—. El único consumo server-side de «activa» era el **defecto del dashboard**
  (`DashboardScopeResolver`), que pasa a usar `FindWorkingSeasonAsync(userId, workspaceId)`. La
  autoselección operativa es puramente un defecto de frontend (`SeasonContext.activeSeason`).
- **«Activar» deja de reabrir.** Antes, activar una cerrada la reabría (invariante «activa ⇒ no
  cerrada»). Ese invariante desaparece: trabajar sobre una cerrada no cambia su estado (CA-4). Reabrir
  sigue siendo una acción explícita del maestro.

### Migración

`SeasonWorkingPerMember` en el orden correcto dentro del `Up` (mismo patrón que
`AddMembershipStatusAndActiveWorkspace`): **add-column → backfill (SQL) → drop-index → drop-column**.

1. `workspace_members.active_season_id` (FK `ON DELETE SET NULL`, índice de apoyo).
2. Backfill: cada miembro hereda la temporada hoy activa de su Workspace
   (`UPDATE … FROM seasons WHERE is_active = true`). Los Workspaces sin activa quedan en `NULL` y
   resuelven el defecto (CA-5).
3. Se retira `ux_seasons_workspace_active` y la columna `seasons.is_active`.
4. Se crea `IX_seasons_workspace_id` (el índice único parcial hacía además de índice de acceso por
   `workspace_id`; al retirarlo, la FK a `workspaces` necesita el índice simple).

En no-desarrollo las migraciones se aplican fuera de banda (no auto-migrate), así que el orden y el
backfill quedan versionados y revisables.

## Contrato

- `GET /seasons` y `GET /seasons/active`: dejan de exponer `is_active`; exponen `is_working` (por el
  usuario que consulta) y `status` con el catálogo nuevo `planificada|abierta|cerrada`.
- `POST /seasons/{id}/activate`: fija la temporada de trabajo **del usuario** (no un flag global; no
  reabre).
- `POST /seasons`: la nueva pasa a ser la de trabajo del creador.
- `GET /dashboard/*` `scope.season`: `is_active` → `status`.
- Catálogo cerrado `season_status`: `planificada`, `abierta`, `cerrada`.

## Arquitectura de la solución

```text
Domain/Seasons/Season.cs               StatusOn(reference); sin IsActive/Activate; Close/Reopen sin activo
Domain/Seasons/SeasonStatus.cs         + Abierta
Domain/Seasons/WorkingSeasonPolicy.cs  regla de defecto (dominio puro)
Domain/Workspaces/WorkspaceMember.cs   ActiveSeasonId + SetActiveSeason
Domain/Seasons/ISeasonRepository       FindWorkingSeasonAsync / SetWorkingSeasonAsync (fuera Find/ActivateExclusively)
Infrastructure/.../SeasonRepository    resolución desde la membresía + defecto; UPDATE por usuario
Application/Seasons/*                   handlers reciben userId; SeasonMapper (Status + is_working)
Application/Dashboard/DashboardScope    defecto = temporada de trabajo del usuario
Infrastructure/.../SeasonWorkingPerMember  migración con backfill
```

Frontend: `SeasonContext.activeSeason` deriva de `is_working`; `season.types` cambia `is_active`→
`is_working` y `status` gana `abierta`; `TemporadasView` muestra estado y «Trabajando aquí» como ejes
separados y «Activar»→«Trabajar en esta»; los modales operativos marcan `· en curso`; el dashboard usa
`status`. Sin cambios de contrato en las escrituras operativas (ya toman `season_id`).

## Estrategia de pruebas

| Nivel | Qué cubre |
|---|---|
| Dominio (`SeasonTests`) | Estado por fechas (abierta/planificada/cerrada, incluida la pasada no cerrada); Close/Reopen sin «activa» |
| Dominio (`WorkingSeasonPolicyTests`) | Regla de defecto: contiene-hoy → abierta reciente → más reciente → null |
| Casos de uso (`Create/Activate/UpdateSeasonHandlerTests`) | Crear y activar fijan la de trabajo del usuario; no tocan la de otros; 404; guardas de nombre |
| SQL real (`SeasonRepositorySqliteTests`) | Resolución desde la membresía + defecto; **aislamiento entre usuarios**; `ON DELETE SET NULL` cae al defecto; orden del listado |
| Dashboard | El defecto usa la temporada de trabajo del usuario |

**Verificación end-to-end conducida**: migración aplicada contra PostgreSQL con **backfill comprobado**
(cada miembro heredó su activa; el Workspace sin activa quedó en `NULL`). Vía API: una campaña pasada no
cerrada se rotula `abierta` (no `planificada`) y una futura `planificada`; `activate` fija mi temporada
de trabajo sin reabrir la cerrada, y la BD confirma que **solo cambió mi membresía**; el dashboard usa
mi temporada de trabajo por defecto. En UI: el maestro muestra el estado y «TRABAJANDO AQUÍ» como ejes
separados, con «Trabajar en esta» / «Cerrar» / «Reabrir» independientes.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Migración destructiva (`drop is_active`) | Backfill **antes** del drop, en el mismo `Up`; versionado y revisable |
| Que alguna escritura operativa dependiera de «activa» | Confirmado que no: todas reciben `season_id` del cliente |
| Estado dependiente de «hoy» | `StatusOn(reference)` recibe la fecha; el dominio no lee el reloj |
| Concurrencia multiusuario | Por membresía: dos usuarios no colisionan; se elimina el índice único de «una activa» |
