---
id: "MVP-701"
tipo: feature
titulo: "Coherencia de contexto: Workspace y temporada"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: ["MVP-104", "MVP-209", "MVP-403"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend", "operativa"]
  modulo_path: "03-modulos/"
  componentes: ["shell", "workspace-context", "season-context", "vistas-operativas"]
  etiquetas: ["mvp", "ajustes", "bug", "coherencia"]
  nivel_riesgo: alto
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-701 — Coherencia de contexto: Workspace y temporada

> **Origen**: `P-081`, `P-082` y `P-083` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

Las tres son la misma pregunta vista desde tres sitios: **que estoy mirando**.

**`P-081`** — Cambiar de Workspace no recarga ninguna vista operativa. La causa esta localizada:
`ApiProvider` memoiza el cliente HTTP con `useMemo(..., [])`, asi que su identidad nunca cambia; de el
cuelgan los `*.service` (`useMemo([http])`) y de estos los `reload` de cada vista. Ninguna cadena
menciona el Workspace, de modo que el efecto de carga no vuelve a dispararse. **Nueve de las diez
vistas operativas no lo mencionan en ninguna dependencia**: Diario, Cosechas, Compras, Terrenos,
Temporadas, Trabajadores, Tareas, Miembros y Vision General. Solo `HomeView`, `AjustesView` y
`SeasonContext` se rehacen con `activeWorkspace?.id`.

Agravante: `switchWorkspace` **si** reemite la sesion en el destino. Durante ese lapso los botones de
corregir y eliminar siguen activos sobre filas del Workspace anterior; el servidor las rechaza con
`AUTH_WORKSPACE_FORBIDDEN`, pero el usuario esta operando sobre datos que ya no son los suyos.

**`P-082`** — `CosechasView`, `DiarioView` y `ComprasView` arrancan con el filtro de temporada en
«todas», mientras `VisionGeneralView` arranca con la temporada de trabajo porque el servidor resuelve
el defecto (`RN-008`, `MVP-209`). Medido en el Workspace de pruebas, el mismo dia y con los mismos
datos: `GET /dashboard/summary` devuelve **4.460,50 kg · 4 partidas · 20,86 L/100kg** y `GET /harvests`
sin filtro devuelve **5.460,5 kg · 5 partidas**, que es lo que rotula la pantalla de Cosechas. Se cuela
una partida de la campana anterior en el total y en la media ponderada. No es una preferencia de
filtro: dos pantallas del producto responden distinto a «cuanto llevo esta campana».

**`P-083`** — La pildora de temporada de `AppTopbar` es un `<span>` decorativo cuando **si** hay
temporada de trabajo, y solo se vuelve pulsable cuando no la hay. Con `P-082` resuelto deja de ser
comodidad: si el defecto de tres vistas pasa a ser la temporada de trabajo, cambiarla tiene que
poderse hacer desde donde se anuncia.

## Objetivo

Que el contexto activo —Workspace y temporada de trabajo— sea uno solo, se anuncie en el shell, se
pueda cambiar desde el shell y gobierne por igual lo que muestran todas las vistas operativas.

## Requisitos de usuario

### HU-1 — Cambiar de Workspace y ver el Workspace nuevo

**Como** persona con varias explotaciones,
**quiero** que al cambiar de Workspace todas las pantallas muestren el nuevo,
**para** no confundir los datos de una finca con los de otra ni actuar sobre los equivocados.

### HU-2 — Fiarme de las cifras de la campana

**Como** titular de la explotacion,
**quiero** que todas las pantallas hablen por defecto de la campana en la que trabajo,
**para** que los totales que veo en una coincidan con los de otra.

### HU-3 — Cambiar de campana sin salir del flujo

**Como** titular de la explotacion,
**quiero** cambiar la temporada de trabajo desde la cabecera,
**para** consultar otra campana sin pasar por el maestro de Temporadas.

## Alcance (in-scope)

- **Invalidacion central** del contexto de datos al cambiar de Workspace, en un unico punto: que la
  identidad del cliente HTTP (o una clave de remontaje del shell) cambie con el Workspace activo, en
  vez de anadir `workspaceId` a nueve efectos.
- Temporada de trabajo como **filtro por defecto** de `DiarioView`, `CosechasView` y `ComprasView`,
  resuelto igual que en el dashboard: el servidor devuelve el ambito aplicado y la UI posiciona el
  control, sin duplicar la regla en el cliente.
- El valor «todas las temporadas» sigue existiendo como eleccion explicita del usuario.
- Conmutador de temporada en la pildora de `AppTopbar`, con la misma forma de interaccion que el
  selector de Workspace, sobre `POST /api/v1/seasons/{id}/activate`, que ya existe.
- Actualizacion de `RN-008` para que deje de hablar solo del dashboard.

## Fuera de alcance (out-of-scope)

- Cambiar el modelo de temporada de trabajo, que `MVP-209` ya dejo por usuario.
- Persistencia de filtros en la URL del diario: es `MVP-705`.
- Recarga en segundo plano o tiempo real de ninguna vista.

## Criterios de aceptación

- [ ] **CA-1**: Tras cambiar de Workspace desde el selector, las diez vistas operativas muestran datos
  del Workspace elegido sin recarga manual. Verificado en UI conducida con dos Workspaces de contenido
  distinto.
- [ ] **CA-2**: Durante y despues del cambio de Workspace no queda ninguna accion (corregir, eliminar,
  alta desde formulario abierto) apuntando a un registro del Workspace anterior.
- [ ] **CA-3**: Sin filtros aplicados, el total de kilos y el numero de partidas de `CosechasView`
  coinciden con los de `GET /dashboard/summary` para la misma temporada de trabajo, comprobado contra
  la API con datos reales.
- [ ] **CA-4**: El diario y el libro de compras aplican por defecto la temporada de trabajo y lo dicen
  en pantalla; «todas las temporadas» sigue siendo elegible.
- [ ] **CA-5**: La pildora de temporada de la cabecera permite cambiar la temporada de trabajo, y
  hacerlo actualiza el contenido de la vista en curso sin recarga.
- [ ] **CA-6**: `RN-008` en `docs/01-producto/reglas-de-negocio.md` describe el defecto aplicado a
  dashboard, diario, cosechas y compras.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/Sidebar.tsx](../../../../../prototype/terrenario-mvp/src/components/Sidebar.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/TopNavbar.tsx](../../../../../prototype/terrenario-mvp/src/components/TopNavbar.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| TopNavbar (pildora de campana) | RN-008, RN-022 | falta | Pendiente |
| Sidebar (selector de Workspace) | RN-034 | parcial | El selector existe; falta que su cambio invalide los datos |
| DiarioView / CosechaModal | RN-008, RN-033 | falta | Pendiente |

## Notas y decisiones

- **Invalidacion central, no parche por vista** (decision del PO): parchear nueve `useEffect` deja la
  trampa puesta para la decima vista que se anada, que volveria a nacer rota.
- El defecto de temporada **se resuelve en servidor**, como ya hace el dashboard: si lo resuelve el
  cliente, la regla vive en dos sitios y vuelve a divergir.
- Esta historia va **primero** en la epica: `MVP-705` y `MVP-707` construyen sobre el contexto que
  deja fijado.
