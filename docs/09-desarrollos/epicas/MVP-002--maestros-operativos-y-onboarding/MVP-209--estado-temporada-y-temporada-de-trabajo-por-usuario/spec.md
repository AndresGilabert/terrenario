---
id: "MVP-209"
tipo: feature
titulo: "Estado de temporada y temporada de trabajo por usuario"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "4d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-203", "MVP-204"]
bloquea: ["MVP-405"]
relacionado_con: ["MVP-201", "MVP-401", "MVP-403"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["temporadas", "workspaces", "contexto-sesion"]
  modulo_path: "03-modulos/"
  componentes: ["temporadas", "workspace-members", "dashboard", "app-shell"]
  etiquetas: ["mvp", "temporadas", "modelo", "correccion", "multiusuario"]
  nivel_riesgo: alto
creado_en: "2026-07-30"
actualizado_en: "2026-07-30"
---

# MVP-209 — Estado de temporada y temporada de trabajo por usuario

## Contexto

Al construir el filtro de temporada del dashboard (`MVP-405`) reapareció `P-045`: una campaña **pasada**
que fue desbancada por «crear cambia la activa» (`P-017`) queda como `is_active=false, is_closed=false`
y hoy se rotula **«planificada»**, que describe algo por venir.

Al analizarlo, el PO (2026-07-30) reformuló el problema de fondo: el maestro de `MVP-203` **fundió dos
conceptos** en el único booleano `Season.is_active` (uno por Workspace):

1. **Estado de la temporada** (informativo): en qué punto de su vida está la campaña.
2. **Temporada de trabajo**: sobre cuál se registra por defecto y se carga al iniciar.

Son ejes distintos. Puedo tener varias campañas **cerradas** (ya no se toca nada), dos **abiertas** —la
actual y una pasada a la que aún llegan registros meses después, p. ej. el rendimiento— y varias
**planificadas** esperando. Sobre todas se puede añadir, editar y borrar (RN-024). La **temporada de
trabajo** es otra cosa: la que se activa por defecto; si necesito registrar en una campaña de hace dos
años la selecciono y se activa, y al cambiar otra vez se activa la nueva. Y **debe ser por usuario**,
para que uno no cambie la temporada sobre la que trabaja otro miembro del mismo Workspace.

Es una corrección de modelo del maestro de `MVP-002` (Hito B/C ya promocionado), priorizada por el PO
antes de cerrar `MVP-405`, porque el dashboard y la autoselección operativa consumen «la temporada
activa». Cierra `P-045`.

## Requisitos de usuario

### HU-1 — Leer el estado real de cada campaña

**Como** miembro del Workspace,
**quiero** que cada temporada muestre su estado real (planificada, abierta o cerrada),
**para** no ver «planificada» en una campaña que en realidad ya pasó.

### HU-2 — Trabajar en una temporada sin pisar a mis compañeros

**Como** miembro del Workspace,
**quiero** elegir sobre qué temporada registro por defecto sin cambiar la de los demás,
**para** poder completar una campaña antigua mientras otro trabaja en la actual.

## Alcance (in-scope)

- **Estado derivado** de la temporada, **independiente** de la de trabajo:
  - `cerrada` — cierre manual (`is_closed`).
  - `abierta` — no cerrada y ya iniciada (`start_date <= hoy`); incluye campañas pasadas no cerradas.
  - `planificada` — no cerrada y aún no iniciada (`start_date > hoy`).
- **Temporada de trabajo por usuario**: cada par (usuario, Workspace) tiene la suya, en
  `workspace_members.active_season_id`. Se resuelve por petición; con defecto cuando no hay fijada.
- «Activar» una temporada pasa a **fijar la de trabajo del usuario** (no un flag global) y **no la
  reabre** si estaba cerrada.
- Crear una temporada la fija como la de trabajo del **creador** (`P-017` pasa a ser por usuario).
- El defecto de temporada del dashboard (RN-008) usa la de trabajo del usuario.
- Migración con backfill: cada miembro hereda la temporada hoy activa de su Workspace, para no cambiar
  el comportamiento visible.

## Fuera de alcance (out-of-scope)

- Rediseño de la UI del maestro de temporadas más allá de separar estado y «trabajando aquí».
- Notificar a otros usuarios el cambio de temporada de trabajo (es por usuario, silencioso).
- Cambios en el flujo de registro operativo: las altas ya reciben `season_id` del cliente.

## Criterios de aceptación

- [ ] **CA-1**: El estado de una temporada se deriva de `is_closed` y de la fecha de inicio frente a
  hoy, sin mirar si es la de trabajo. Una campaña pasada no cerrada se muestra `abierta`, no
  `planificada`; una futura no iniciada, `planificada`.
- [ ] **CA-2**: La temporada de trabajo es **por usuario**: fijar la de un usuario (activar o crear) no
  altera la de otro miembro del mismo Workspace.
- [ ] **CA-3**: Sin temporada de trabajo fijada, se resuelve un defecto sensato (campaña abierta que
  contiene hoy, si la hay) y el dashboard y los formularios operativos lo usan como autoselección.
- [ ] **CA-4**: «Activar» una temporada cerrada la fija como de trabajo **sin reabrirla**; reabrir sigue
  siendo una acción explícita del maestro.
- [ ] **CA-5**: La migración conserva el comportamiento visible: tras aplicarla, cada miembro tiene como
  temporada de trabajo la que era la activa de su Workspace.

## Maquetas y referencias visuales

- Reutiliza el maestro existente [prototype/terrenario-mvp/src/components/TemporadasView.tsx](../../../../../prototype/terrenario-mvp/src/components/TemporadasView.tsx).

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| TemporadasView | RN-024, nuevo estado `abierta` | falta | La insignia muestra el estado real, desacoplado de la de trabajo |
| TemporadasView / TopNavbar | RN-021, RN-022 (reescrita) | falta | «Trabajando aquí» por usuario; activar fija la de trabajo sin reabrir |

## Notas y decisiones

- **Cierra `P-045`** con la separación estado/trabajo, en vez de renombrar un valor.
- **RN-022 se reescribe** (de «una activa por Workspace» a «temporada de trabajo por usuario»); se
  retira el índice único parcial `ux_seasons_workspace_active`. RN-021 (autoselección) se mantiene con
  la nueva semántica.
- **La temporada de trabajo se resuelve por petición**, no viaja en el JWT: es un defecto/preferencia,
  no gobierna el aislamiento como el `workspace_id`, así que meterla en el token solo añadiría
  reemisiones (decisión de diseño, ver `tech-design.md`).
- **Riesgo acotado**: ninguna escritura operativa resuelve «activa» en servidor —las altas reciben
  `season_id`—, así que la autoselección es puramente un defecto de frontend.
