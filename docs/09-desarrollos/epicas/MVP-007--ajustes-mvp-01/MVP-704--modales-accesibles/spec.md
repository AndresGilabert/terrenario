---
id: "MVP-704"
tipo: feature
titulo: "Modales accesibles"
estado: completado
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "accesibilidad", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["modales", "formularios", "a11y"]
  etiquetas: ["mvp", "ajustes", "a11y", "reabierto"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-704 — Modales accesibles

> **Origen**: `P-055` (reabierto) del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

`P-055` se registro en `MVP-304` con destino `MVP-502` y estado «aprobado-crear-historia». **`MVP-502`
se cerro sin hacerlo**: su alcance era hardening de seguridad y validacion de PII, no accesibilidad,
asi que el punto nunca tuvo historia que lo construyera. El destino se anoto y nadie lo recogio.

Verificado en el codigo entregado: no existe ningun componente `Modal` comun, y `ActivityFormModal`,
`HarvestFormModal`, `PurchaseFormModal`, `ConsumptionFormModal`, `PlotFormModal`, `WorkerFormModal` y
`SeasonFormModal` **no tienen ni una sola** ocurrencia de `role="dialog"`, `aria-modal` ni manejo de
`Escape`. Solo `ConfirmDialog` y algun modal informativo lo tienen.

El defecto que origino el punto sigue reproduciendose: con un modal abierto, los controles del fondo
siguen siendo alcanzables con el tabulador y **se pueden activar**, de modo que pulsar el envio del
formulario en linea del fondo dispara el alta equivocada. No es solo accesibilidad: es un defecto
funcional.

## Objetivo

Que un modal abierto sea el unico contexto interactivo de la pantalla, para cualquier forma de manejo:
raton, teclado o lector de pantalla.

## Requisitos de usuario

### HU-1 — No disparar una accion que no se pretendia

**Como** persona registrando datos,
**quiero** que con un formulario abierto no pueda activarse nada del fondo,
**para** no dar de alta por error un registro distinto del que estaba escribiendo.

### HU-2 — Manejar el producto con teclado

**Como** persona que navega con teclado o lector de pantalla,
**quiero** que el foco se quede dentro del dialogo y vuelva de donde salio al cerrarlo,
**para** poder usar el producto sin raton.

## Alcance (in-scope)

- Componente `Modal` comun con: trampa de foco, cierre con `Escape`, `role="dialog"` y `aria-modal`,
  etiquetado accesible del titulo y restauracion del foco al control que lo abrio.
- Migracion de los siete modales de formulario al componente comun.
- Revision de `ConfirmDialog`, `InvitationModal`, `TerrenoDetailModal` y `CloseWorkspaceModal` para que
  usen la misma base y no queden dos formas de hacer lo mismo.
- Bloqueo efectivo de la interaccion con el fondo mientras el modal esta abierto.

## Fuera de alcance (out-of-scope)

- Auditoria de accesibilidad completa del producto (contraste, encabezados, formularios). Aqui solo se
  cierra el hallazgo de los modales.
- Redisenar el contenido de los formularios.

## Criterios de aceptación

- [x] **CA-1**: Con cualquiera de los modales abierto, tabular no alcanza ningun control del fondo y el
  foco cicla dentro del dialogo. Lo garantiza `inert` sobre `#root` —que apaga el fondo entero, no solo
  el tabulador— mas una trampa que cierra el ciclo en los extremos. Medido sobre la aplicacion en
  marcha con el modal de correccion de compra abierto: de los **30 controles del fondo, 0** pueden
  recibir foco, y tras **40 pulsaciones reales** de tabulador el foco sigue dentro del dialogo.
- [x] **CA-2**: `Escape` cierra cualquier modal del producto, de forma uniforme. Lo hace el componente
  comun, asi que ya no hay modales que lo tengan y modales que no: antes solo lo tenia
  `InvitationModal`. Se anade una excepcion deliberada —no cierra mientras hay un envio en curso—,
  descrita en el TDD.
- [x] **CA-3**: Al cerrar, el foco vuelve al control que abrio el modal. Verificado en UI conducida:
  tras `Escape`, el foco esta de vuelta en «Corregir la compra de Abono NPK». Se comprueba
  `document.contains` antes de devolverlo, porque el disparador puede haber desaparecido con la fila
  que se acaba de borrar.
- [x] **CA-4**: Los once modales exponen `role="dialog"`, `aria-modal="true"` y un nombre accesible.
  Abiertos **uno a uno en la aplicacion**, no deducidos del codigo: «Corregir compra», «Imputar compra
  a un terreno», «¿Eliminar la compra?», «Añadir nuevo terreno», «Detalle del terreno La Via»,
  «Registrar cosecha», «Nueva actividad», «Añadir trabajador», «Nueva temporada», «Dar de baja el
  Workspace» y «Tienes una invitacion». Los once, ademas, fuera de `#root` y con `#root` inerte.
- [x] **CA-5**: Reproducido y corregido el defecto original: con un modal abierto, enviar el formulario
  en linea del fondo ya no es posible. El escenario es literalmente el que lo origino —la vista de
  compras tiene el alta en linea detras—: con el modal abierto, `document.elementFromPoint` sobre las
  coordenadas del boton de envio del fondo devuelve **el velo**, y el boton no puede recibir foco.
- [x] **CA-6**: Cobertura de test del componente comun (trampa de foco, `Escape`, restauracion). Trece
  tests en `Modal.test.tsx`, que cubren ademas el defecto de `P-055` con un formulario en linea de
  fondo y el caso de **dos modales apilados** —el contador, no un booleano—. `jsdom` no implementa
  `inert`: alli se comprueba que el control del fondo cae dentro del subarbol marcado y que el dialogo
  no, y el efecto real lo cierra la UI conducida.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| CosechaModal y equivalentes | docs/04-ingenieria/estandares-codigo.md | hecho | Los once modales usan `components/common/Modal.tsx`; abiertos uno a uno en la aplicacion con `role`, `aria-modal`, nombre accesible y fondo inerte |

## Notas y decisiones

- **Este punto se perdio una vez.** Es el motivo del `CA-6` de la epica: ningun punto puede apoyarse en
  «lo hara la historia de al lado» si esa historia no lo tiene en su alcance escrito.
- El PO descarto la variante «solo la parte funcional» (bloquear el fondo sin el trabajo de a11y):
  dejaba fuera teclado y lector de pantalla, que es el grueso del punto.
- **Habia tres formas distintas de hacer lo mismo**, no una y ocho ausencias: `ConfirmDialog` ponia
  `role`/`aria-modal` en el **velo** en vez de en el panel, `InvitationModal` tenia su propio `Escape`
  y su propio clic en el velo, y `TerrenoDetailModal` y `CloseWorkspaceModal` cerraban con `onClick` en
  el velo. Unificarlas era la mitad del valor de la historia.
- **Dos cierres dejan de responder al clic fuera**: `ConfirmDialog` y `CloseWorkspaceModal`. Son
  decisiones que se acaban de pedir de forma explicita y la segunda incluye elegir destinatario;
  descartarlas por un clic despistado es peor que exigir `Escape` o «Cancelar».
- **El *drawer* de navegacion de movil de `AppLayout` queda fuera** y se registra en `MVP-999`: es el
  ultimo overlay sin trampa de foco, pero su forma es otra —lateral, sin titulo ni cabecera— y meterlo
  en `Modal` obligaria a un modo «lateral» que condiciona el componente por un solo uso.
- **`NotificationBell` no se toca**: es un popover anclado, no un modal. Tiene `role="dialog"` sin
  `aria-modal` y **no debe** atrapar el foco.
