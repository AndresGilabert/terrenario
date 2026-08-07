---
id: "MVP-702"
tipo: feature
titulo: "Aprovechamiento del espacio: escritorio y movil"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: ["MVP-701"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["shell", "layout", "listados", "dashboard"]
  etiquetas: ["mvp", "ajustes", "ux", "responsive"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-702 — Aprovechamiento del espacio: escritorio y movil

> **Origen**: `P-086` y `P-090` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

Son la misma composicion vista en los dos extremos.

**`P-086`** — `AppLayout` encierra **todas** las secciones en un contenedor `max-w-3xl` (768 px).
Verificado en navegador a 1920x1000: con el lateral de ~256 px, el contenido ocupa 768 px y el resto es
fondo. Duele donde hay tabla —Cosechas, Terrenos, Trabajadores, Miembros— y en la rejilla
`lg:grid-cols-4` del dashboard, que apretuja cuatro tarjetas en 768 px porque el breakpoint de Tailwind
mira el **viewport**, no el contenedor.

El contenedor unico se introdujo en `P-016` para dar coherencia de tamano y espaciado entre secciones,
y ese objetivo sigue siendo correcto. Lo que hay que revisar es la cota, no retirarla.

**`P-090`** — En movil esa misma composicion se apila en vez de repartirse. Verificado a 375x812 sobre
Cosechas: cabecera de seccion, tarjeta de titulo con descripcion, boton de alta, **tres** tarjetas de
resumen a ancho completo y **tres** filtros suman ~780 px antes de la primera fila de datos. La
pantalla completa se ocupa con contexto. El mismo patron esta en Diario (cinco tarjetas de resumen y
cinco filtros) y en Compras.

## Objetivo

Que el producto use el espacio disponible: que un listado se lea entero en escritorio y que en movil
se vean datos antes de tener que desplazarse.

## Requisitos de usuario

### HU-1 — Leer un listado sin que sobre pantalla

**Como** titular de la explotacion en el ordenador,
**quiero** que las tablas usen el ancho que hay,
**para** ver una partida completa sin desplazamiento horizontal ni columnas apretadas.

### HU-2 — Ver los datos nada mas entrar en el movil

**Como** titular de la explotacion en el campo,
**quiero** que lo primero que vea sea el registro, no el resumen y los filtros,
**para** consultar lo que busco sin recorrer una pantalla entera.

## Alcance (in-scope)

- Dos cotas de ancho en lugar de una, por **tipo de contenido**: listados y dashboard anchos,
  formularios y pantallas de lectura estrechos. La coherencia entre secciones del mismo tipo se
  mantiene, que es lo que buscaba `P-016`.
- Revision de la rejilla del dashboard para que el numero de columnas case con el ancho real
  disponible, no con el viewport.
- En movil: resumen en una fila desplazable o plegable y filtros detras de un desplegable, dejando los
  datos arriba. Aplicado a Diario, Cosechas y Compras.

## Fuera de alcance (out-of-scope)

- Redisenar los componentes de tarjeta, tabla o formulario mas alla de su contenedor.
- Cambiar la navegacion lateral, que `MVP-406` ya dejo agrupada y con seccion activa.
- Vistas nuevas o columnas nuevas en los listados.

## Criterios de aceptación

- [ ] **CA-1**: A 1920 px de ancho, los listados y el dashboard ocupan un ancho util acorde a su
  contenido, y ninguna tabla del producto exige desplazamiento horizontal para leer sus columnas.
- [ ] **CA-2**: Los formularios y pantallas de lectura conservan una medida de linea comoda: abrir el
  ancho no puede producir campos de texto de 1.000 px.
- [ ] **CA-3**: Las secciones del mismo tipo comparten ancho y espaciado entre si (invariante de
  `P-016`).
- [ ] **CA-4**: A 375 px, en Diario, Cosechas y Compras se ve al menos un registro sin desplazarse.
- [ ] **CA-5**: Resumen y filtros siguen siendo alcanzables en movil sin exigir mas de una accion.
- [ ] **CA-6**: Verificado en navegador real a 375, 768, 1280 y 1920 px, con captura de cada uno.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| DashboardView | RN-005 (pantalla unica con scroll vertical) | parcial | La pantalla es unica; el ancho no acompana |
| DiarioView | RN-033 | falta | Pendiente |

## Notas y decisiones

- Va **despues** de `MVP-701`: reordenar componentes cuya carga aun va a cambiar obliga a tocarlos dos
  veces.
- `RN-005` («dashboard en pantalla unica con scroll vertical») no se toca: sigue siendo una sola
  pantalla, solo que mejor repartida.
