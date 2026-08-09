---
id: "MVP-702"
tipo: feature
titulo: "Aprovechamiento del espacio: escritorio y movil"
estado: completado
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

- [x] **CA-1**: A 1920 px de ancho, los listados y el dashboard ocupan un ancho util acorde a su
  contenido. Medido: el contenedor de Cosechas pasa de 768 a **1280 px**, la tabla ocupa 1278 **sin
  desplazamiento horizontal**, y el resumen del panel pinta **4 columnas** de 308 px.
  Matiz honesto: **a 768 px exactos** una tabla ancha si sigue pidiendo desplazamiento dentro de su
  tarjeta (contenedor 448, tabla 917). Es **anterior a esta historia** —medido igual en `develop`— y su
  causa es que a 768 aparece el lateral y se lleva 256 px; corregirlo exige mover su punto de corte,
  que esta fuera de alcance. Registrado para decidirlo aparte.
- [x] **CA-2**: Los formularios y pantallas de lectura conservan una medida de linea comoda. Ajustes se
  queda en **768 px** con la pantalla a 1920, y su campo de texto mide 718 px.
- [x] **CA-3**: Las secciones del mismo tipo comparten ancho y espaciado entre si. La cota la decide un
  **unico mapa ruta → ancho** en el shell, no cada vista: asi una pantalla nueva hereda la medida de su
  tipo sin decidir nada. Comprobado que Cosechas y Terrenos dan 1280 px, y Ajustes 768.
- [x] **CA-4**: A 375 px, en Diario, Cosechas y Compras se ve al menos un registro sin desplazarse.
  Primera fila en **y=530** (Cosechas), **y=291** (Diario) y **y=497** (Compras) sobre 812 de alto. En
  Compras hizo falta plegar tambien el alta en linea: sin eso la fila caia en `y=802`, visible sobre el
  papel e invisible en la practica.
- [x] **CA-5**: Resumen y filtros siguen siendo alcanzables en movil sin exigir mas de una accion. El
  resumen **no se pliega** —queda como fila desplazable, porque es lo que se mira al entrar— y los
  filtros estan a una pulsacion, con un contador que dice cuantos hay puestos y que los abre de entrada
  si acotan lo que se ve.
- [~] **CA-6**: Verificado en navegador real a 375, 768, 1280 y 1920 px. **Las capturas no existen**: el
  panel del navegador no se puede mostrar en este entorno. En su lugar hay medidas tomadas del motor de
  CSS en los cuatro anchos —contenedor, tabla, columnas, desplazamiento y posicion de la primera fila—,
  que para lo que pregunta el criterio son evidencia mas fuerte que una imagen. Queda pendiente la
  captura si se considera imprescindible.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| DashboardView | RN-005 (pantalla unica con scroll vertical) | hecho | Sigue siendo una sola pantalla; las columnas las decide ahora el ancho real disponible |
| DiarioView | RN-033 | hecho | Resumen en fila desplazable y filtros plegados en movil; primera entrada visible a 375 px |

## Notas y decisiones

- Va **despues** de `MVP-701`: reordenar componentes cuya carga aun va a cambiar obliga a tocarlos dos
  veces.
- `RN-005` («dashboard en pantalla unica con scroll vertical») no se toca: sigue siendo una sola
  pantalla, solo que mejor repartida.
