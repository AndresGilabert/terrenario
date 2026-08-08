---
id: "MVP-704"
tipo: feature
titulo: "TDD: Modales accesibles"
estado: completado
tickets: []
epica: "MVP-007--ajustes-mvp-01"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "accesibilidad", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["modales", "formularios", "a11y"]
  etiquetas: ["mvp", "ajustes", "a11y", "reabierto"]
  nivel_riesgo: medio
creado_en: "2026-08-08"
actualizado_en: "2026-08-08"
---

# TDD: MVP-704 — Modales accesibles

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Un componente, `src/frontend/terrenario-web/src/components/common/Modal.tsx`, y once llamadas. Antes
había **tres formas distintas** de hacer lo mismo y ocho de no hacerlo:

| Modal | Antes | Ahora |
|---|---|---|
| Los siete de formulario | `<div className="fixed inset-0 …">` pelado: ni `role`, ni `aria-modal`, ni `Escape` | `Modal` |
| `ConfirmDialog` | `role`/`aria-modal` en el **velo**, no en el panel; sin `Escape` ni trampa | `Modal` |
| `InvitationModal` | `Escape` propio, velo con `role="presentation"`, panel con `role="dialog"` | `Modal` |
| `TerrenoDetailModal` | `onClick` en el velo + `stopPropagation` en el panel; sin `Escape` | `Modal` |
| `CloseWorkspaceModal` | Velo hermano del panel con `onClick`; sin `Escape` | `Modal` |

El overlay `fixed inset-0` deja de existir fuera del componente: el único que queda en `src/` es el
del propio `Modal` (y el *drawer* de navegación de `AppLayout`, que no es un diálogo — ver más abajo).

## Las tres piezas, y por qué hacen falta las tres

### 1. `inert` sobre `#root` — la que arregla el defecto funcional

`P-055` no era solo accesibilidad. El velo tapaba **visualmente**, pero el fondo seguía vivo: sus
controles recibían foco y podían activarse. Con un modal abierto sobre la vista de compras —que tiene
el alta en línea justo detrás— pulsar su envío daba de alta la compra equivocada.

`inert` es lo único que apaga el fondo entero de una vez: tabulador, clic, búsqueda del navegador y
recorrido de lector de pantalla. Un `tabIndex={-1}` por control no habría cubierto el clic, y ocultar
con `aria-hidden` no habría cubierto el tabulador.

Consecuencia de diseño obligada: **el diálogo tiene que vivir fuera del árbol que se apaga**. De ahí
`createPortal(…, document.body)`. Si se pintara donde está declarado, quedaría dentro de `#root` y se
apagaría a sí mismo.

Un **contador** de modales abiertos, no un booleano: `ConfirmDialog` sobre un formulario es un caso
real —confirmar un borrado desde el modal de corrección— y con un booleano el primero en cerrarse
devolvería la vida al fondo con el otro todavía abierto.

### 2. Trampa de foco

`inert` ya impide salir hacia el fondo, pero sin ciclar, el tabulador se escapa a la barra del
navegador y volver cuesta. Se interviene **solo en los extremos** de la lista de enfocables: en medio,
el orden natural del navegador ya es el correcto y reimplementarlo solo introduce diferencias.

**Foco inicial en el primer control del cuerpo, no del panel.** El primero del panel es siempre el
aspa de cerrar de la cabecera; dejar el foco ahí al abrir un formulario convierte el primer `Intro` en
un cierre. Si el cuerpo declara `autoFocus` —lo hacen varios—, se respeta esa intención por delante
del orden del DOM; es como `ConfirmDialog` mantiene el foco en «Cancelar» y no en «Eliminar».

Para poder distinguir cuerpo de cabecera sin cambiar la maquetación, el cuerpo va envuelto en un `div`
con `contents`: no crea caja, así que los hijos siguen siendo hijos directos del panel flex y sus
alturas y zonas desplazables no cambian.

### 3. Restauración del foco

Al cerrar, el foco vuelve al control que abrió el modal. Si no, aterriza en el `body` y quien navega
con teclado tiene que rehacer todo el camino. Se guarda **antes** de moverlo y se comprueba
`document.contains` antes de devolverlo: el disparador puede haber desaparecido con la fila que se
acaba de borrar, y `focus()` sobre un nodo desconectado no hace nada.

## Decisiones que cambian comportamiento previo

- **`Escape` y el clic fuera se bloquean mientras hay un envío en curso** (`closeDisabled`). Antes solo
  se deshabilitaba el aspa, así que `Escape` podía descartar un formulario que ya estaba guardándose y
  dejar al usuario sin saber si se guardó.
- **El clic fuera solo cierra si nace y muere en el velo** (`onMouseDown` con `target === currentTarget`).
  Arrastrar una selección de texto desde dentro del panel y soltar fuera no es un clic fuera; con el
  `onClick` anterior, ese gesto cerraba el formulario y perdía lo escrito.
- **`ConfirmDialog` y `CloseWorkspaceModal` dejan de cerrarse al pulsar fuera.** Son decisiones que se
  acaban de pedir de forma explícita —y la segunda incluye elegir destinatario—: descartarlas por un
  clic despistado es peor que exigir `Escape` o «Cancelar». Las otras dos vías siguen ahí.
- **El nombre accesible se resuelve de dos maneras, no de una.** Con la cabecera por defecto sale del
  `h3` que ya se ve (`aria-labelledby` con `useId`, no un identificador fijo que se duplicaría con dos
  diálogos vivos). Con cabecera propia se pone como `aria-label`, que evita tener que pintar un texto
  oculto que repita el que ya está en pantalla.

## Fuera de alcance, y por qué

- **El *drawer* de navegación de móvil** (`AppLayout`) es el último overlay sin trampa de foco del
  producto. No se ha migrado: su forma es otra —panel lateral a sangre, sin título ni cabecera— y
  meterlo en `Modal` obligaría a un modo «lateral» que condiciona el componente por un solo uso. Queda
  registrado en `MVP-999`.
- **`NotificationBell` no es un modal** y no se ha tocado. Es un popover anclado al botón: tiene
  `role="dialog"` sin `aria-modal`, cierra con `Escape` y con clic fuera, y **no debe** atrapar el foco.
  Es correcto como está.

## Verificación

Trece tests de `Modal.test.tsx` (`vitest` + `@testing-library`) cubren el `CA-6`, incluida la
reproducción del defecto de `P-055` con un formulario en línea de fondo, y el caso de dos modales
apilados. `jsdom` no implementa `inert`, así que ahí se comprueba lo comprobable —que el control del
fondo cae dentro del subárbol marcado y que el diálogo no— y el efecto real se cierra en UI conducida.

Sobre la aplicación en marcha, con el modal de corrección de compra abierto sobre la vista de compras
(`CA-5`, el escenario original):

| Comprobación | Resultado |
|---|---|
| `#root` marcado como inerte | sí |
| Diálogo fuera de `#root`, en `body` | sí |
| Controles del fondo | 30 |
| De ellos, capaces de recibir foco | **0** |
| Qué hay en el punto del envío del fondo | el velo, no el botón |
| Foco tras 40 tabuladores reales | sigue dentro del diálogo |
| `Escape` | cierra; `inert` y el desplazamiento del fondo se devuelven |
| Foco tras cerrar | de vuelta en «Corregir la compra de Abono NPK» |

Y los **once** abiertos uno a uno en la aplicación, cada uno con `role="dialog"`, `aria-modal="true"`,
nombre accesible y `#root` inerte (`CA-4`): «Corregir compra», «Imputar compra a un terreno»,
«¿Eliminar la compra?», «Añadir nuevo terreno», «Detalle del terreno La Via», «Registrar cosecha»,
«Nueva actividad», «Añadir trabajador», «Nueva temporada», «Dar de baja el Workspace» y «Tienes una
invitación» —esta última con una invitación pendiente creada para la prueba y borrada al terminar—.
