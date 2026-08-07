---
id: "MVP-704"
tipo: feature
titulo: "Modales accesibles"
estado: borrador
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
actualizado_en: "2026-08-07"
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

- [ ] **CA-1**: Con cualquiera de los modales abierto, tabular no alcanza ningun control del fondo y el
  foco cicla dentro del dialogo.
- [ ] **CA-2**: `Escape` cierra cualquier modal del producto, de forma uniforme.
- [ ] **CA-3**: Al cerrar, el foco vuelve al control que abrio el modal.
- [ ] **CA-4**: Los once modales exponen `role="dialog"`, `aria-modal="true"` y un nombre accesible.
- [ ] **CA-5**: Reproducido y corregido el defecto original: con un modal abierto, enviar el formulario
  en linea del fondo ya no es posible. Verificado en UI conducida.
- [ ] **CA-6**: Cobertura de test del componente comun (trampa de foco, `Escape`, restauracion).

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| CosechaModal y equivalentes | docs/04-ingenieria/estandares-codigo.md | falta | Ningun modal de formulario lo cumple hoy |

## Notas y decisiones

- **Este punto se perdio una vez.** Es el motivo del `CA-6` de la epica: ningun punto puede apoyarse en
  «lo hara la historia de al lado» si esa historia no lo tiene en su alcance escrito.
- El PO descarto la variante «solo la parte funcional» (bloquear el fondo sin el trabajo de a11y):
  dejaba fuera teclado y lector de pantalla, que es el grueso del punto.
