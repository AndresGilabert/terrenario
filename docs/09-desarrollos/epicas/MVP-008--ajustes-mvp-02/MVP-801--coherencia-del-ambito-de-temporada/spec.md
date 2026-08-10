---
id: "MVP-801"
tipo: bugfix
titulo: "Coherencia del ambito de temporada"
estado: aprobado
prioridad: alta
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: []
bloquea: ["MVP-802"]
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend", "backend"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "diario", "temporadas"]
  etiquetas: ["mvp", "ajustes", "bug", "contexto"]
  nivel_riesgo: medio
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-801 — Coherencia del ambito de temporada

> **Origen**: `P-107` y `P-108` del registro de `MVP-999`, detectados en la segunda revision completa
> del MVP (2026-08-10) y clasificados por el Product Owner.

## Contexto

`RN-008` dice que un `season_id` que no exista en el Workspace **cae al defecto**, y dice ademas por
que existe la regla: «desde `MVP-705` el filtro viaja en la URL y al cambiar de Workspace puede quedar
el de otro». El diario, cosechas y compras la cumplen. La Vision General no.

**`P-107`** — `GET /api/v1/dashboard/summary?season_id=<de otro Workspace>` responde `200` con
`scope.season: null` y todos los agregados a cero, en vez de resolver el defecto. En pantalla, estando
en un Workspace con tres campanas, cuatro partidas vivas y 4.460,50 kg, la Vision General muestra el
estado vacio **«Todavia no hay temporada que mirar. Crea o activa una temporada»** mientras su propio
selector lista las tres campanas. Basta con cambiar de Workspace desde esa pantalla para provocarlo:
`switchWorkspace` reemite la sesion y remonta el area operativa, pero no limpia la query. El mismo
camino afecta a `plot_ids`: con terrenos de otro Workspace el ambito queda en `plots: 0`.

**`P-108`** — Con `/app/diario?season_id=<de otro Workspace>` el servidor **si** aplica el defecto
(devuelve la campana de trabajo y un solo registro de los 35 del Workspace), pero el `<select>` muestra
**«Todas las temporadas»**. La pantalla afirma algo falso sobre lo que se esta viendo, que es peor que
no decir nada. La causa esta en `lib/season-scope.ts`: `value` da por buena cualquier seleccion
explicita, y desde `MVP-705` la del diario viene de la URL; como ese identificador no esta entre las
opciones, el control cae en la primera. Cosechas y compras no lo sufren porque su seleccion vive en
estado local y arranca vacia.

Van juntas porque son el mismo escenario visto por los dos lados —lo que el servidor decide y lo que el
cliente dice que ha decidido— y el arreglo vive en la misma pieza conceptual.

## Objetivo

Que ninguna vista operativa muestre un ambito distinto del que aplica, y que un filtro heredado de otro
Workspace deje de vaciar la pantalla insignia del producto.

## Requisitos de usuario

### HU-1 — No me digas que cree lo que ya tengo

**Como** titular de la explotacion,
**quiero** que al cambiar de Workspace la Vision General me ensene la campana de ese Workspace,
**para** no leer que no tengo temporadas cuando el selector de al lado me esta listando tres.

### HU-2 — Que la pantalla diga lo que esta mostrando

**Como** titular de la explotacion,
**quiero** que el filtro de temporada refleje la campana que de verdad se esta aplicando,
**para** no interpretar mal unas cifras creyendo que son de todas las campanas.

## Alcance (in-scope)

- Aplicar en los cinco endpoints de `dashboard` la misma resolucion de ambito que ya usan diario,
  cosechas y compras: un `season_id` desconocido en el Workspace **cae al defecto** de `RN-008` en vez
  de devolver `season: null` y ceros.
- Mismo criterio para `plot_ids`: identificadores que no pertenezcan al Workspace activo se descartan y
  el ambito cae en «todos los terrenos activos», en vez de dejar la seleccion vacia.
- Reconciliar en el cliente la seleccion con el ambito aplicado: si el servidor informa de un ambito
  distinto del que pedia la URL, manda el del servidor y la URL se corrige.
- Limpiar los parametros de ambito de la URL al cambiar de Workspace, para que el escenario no llegue
  siquiera a producirse.
- Precisar `RN-008`: la caida al defecto aplica **tambien al dashboard**, y el ambito devuelto por el
  servidor manda sobre la seleccion que traiga la URL.
- Regresion en el cliente que fije el caso de `P-108` (identificador ausente de las opciones) y prueba
  de integracion que fije el de `P-107`.

## Fuera de alcance (out-of-scope)

- **`season_id=all` en el dashboard**: sigue respondiendo `400`. `RU-38` acota el analisis a una sola
  campana y esta historia no lo cambia.
- Llevar los filtros de cosechas y compras a la URL: es `MVP-802`, y va despues.
- Cualquier cambio en los widgets, sus calculos o su presentacion.

## Criterios de aceptación

- [ ] **CA-1**: `GET /api/v1/dashboard/*` con un `season_id` que no pertenezca al Workspace activo
  devuelve el **mismo** `scope.season` que `GET /api/v1/diary` con ese mismo identificador, en vez de
  `null`. Verificado con los dos endpoints en la misma llamada de prueba.
- [ ] **CA-2**: Con terrenos de otro Workspace en `plot_ids`, el ambito cae en todos los terrenos
  activos del Workspace activo y el resumen deja de salir a cero.
- [ ] **CA-3**: Cambiar de Workspace estando en la Vision General con filtros en la URL deja la
  pantalla mostrando datos del Workspace elegido, sin estado vacio y sin recarga manual.
- [ ] **CA-4**: En el diario, con un `season_id` desconocido en la URL, el `<select>` de temporada
  muestra **la campana que el servidor ha aplicado**, no «Todas las temporadas», y la URL queda
  corregida.
- [ ] **CA-5**: `RN-008` recoge las dos precisiones y no queda ninguna vista operativa exenta de la
  regla.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Notas y decisiones

- **El defecto se resuelve en servidor, no en el cliente.** `RN-008` lo dice expresamente: «si el
  cliente resolviera el defecto, la regla viviria en dos sitios y volveria a divergir». Esta historia
  no puede cerrarse anadiendo la comprobacion en `VisionGeneralView`.
- **Limpiar la URL al cambiar de Workspace no sustituye al arreglo del servidor.** Un enlace compartido
  o un marcador reproducen el escenario sin pasar por el selector; hacen falta las dos cosas.
- Va **antes** de `MVP-802`: llevar los filtros de cosechas y compras a la URL es justo lo que expone
  este defecto, asi que hacerlo primero lo propagaria a dos vistas mas.
