---
id: "MVP-702"
tipo: feature
titulo: "TDD: Aprovechamiento del espacio: escritorio y movil"
estado: completado
tickets: []
epica: "MVP-007--ajustes-mvp-01"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["shell", "layout", "listados", "dashboard"]
  etiquetas: ["mvp", "ajustes", "ux", "responsive"]
  nivel_riesgo: medio
creado_en: "2026-08-08"
actualizado_en: "2026-08-08"
---

# TDD: MVP-702 — Aprovechamiento del espacio: escritorio y móvil

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

La misma composición vista en los dos extremos, y en los dos el arreglo es de **contenedor**, no de
contenido:

| Punto | Antes | Ahora |
|---|---|---|
| `P-086` (escritorio) | Un único `max-w-3xl` (768 px) para todo; a 1920 el resto era fondo | Dos cotas **por tipo de contenido**: 1280 px para listados y panel, 768 para formularios y lectura |
| `P-086` (rejilla) | `lg:grid-cols-4` miraba el **viewport**, así que a 1920 pedía cuatro columnas y las metía en 768 px | Consultas de contenedor (`@2xl`/`@5xl`): las columnas las decide el ancho **real disponible** |
| `P-090` (móvil) | Resumen y filtros apilados empujaban los datos bajo el pliegue | Resumen en fila desplazable; filtros —y el alta de compra— plegados a una acción |

**No se retira el contenedor único, se le pone una cota más.** El objetivo de `P-016` —coherencia de
tamaño y espaciado entre secciones— sigue siendo correcto; lo que había que revisar era la cota. Por
eso son **dos** y no ninguna: la coherencia se mantiene dentro de cada tipo, que es la invariante que
`P-016` protegía (CA-3).

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
|---|---|---|
| `frontend/.../layout/AppLayout.tsx` | modificado | Mapa ruta → ancho y `@container` como raíz de consulta |
| `frontend/.../common/SummaryStrip.tsx` | nuevo | Resumen: fila desplazable en móvil, rejilla en escritorio |
| `frontend/.../common/MobileDisclosure.tsx` | nuevo | Bloque plegado en móvil, a la vista en escritorio |
| `frontend/.../common/MobileDisclosure.test.tsx` | nuevo | Cobertura, con el caso del árbol duplicado |
| `frontend/.../lib/use-media-query.ts` | nuevo | Elegir **un** árbol cuando cambia la estructura, no pintar dos |
| `frontend/.../{diary,harvests,purchases}/*View.tsx` | modificado | Consumen los dos envoltorios |
| `frontend/.../dashboard/VisionGeneralView.tsx` | modificado | Rejillas por consulta de contenedor |
| `frontend/.../test/setup.ts` | modificado | `matchMedia` en jsdom |

## Diseño detallado

### El mapa de anchos vive en el shell

`anchoParaRuta` está en `AppLayout`, junto a `titleForPath`, y no en cada vista **a propósito**: si
cada pantalla eligiera su ancho, la coherencia duraría hasta la siguiente que se añadiera. Con el mapa
en un sitio, una vista nueva hereda la medida de su tipo sin decidir nada.

- **Ancho** (`max-w-7xl`, 1280 px): listados y panel. Su contenido son tablas y rejillas.
- **Estrecho** (`max-w-3xl`, 768 px): formularios, ajustes y el checklist del Home. Aquí ensanchar
  **empeora**: un campo de texto de 1.000 px o un párrafo de 200 caracteres por línea se leen peor
  (CA-2).

### Consultas de contenedor, no de viewport

`lg:grid-cols-4` preguntaba por el tamaño de la **pantalla**. Con el contenido dentro de un contenedor
acotado, esa pregunta no era la relevante: a 1920 px pedía cuatro columnas y las metía en 768. Con
`@container` en `main` y `@2xl`/`@5xl` en las rejillas, la pregunta pasa a ser la correcta —cuánto
sitio hay de verdad—, y sigue funcionando si mañana cambia la cota o aparece otro panel lateral.

### Un solo árbol, no dos

Es la decisión con más contenido de la historia, y viene de un error propio: la primera versión de los
dos envoltorios pintaba los hijos **dos veces** —uno con `sm:hidden` y otro con `hidden sm:block`— y
ocultaba uno con CSS.

Se veía perfecto y estaba roto: metía en el DOM **dos copias de cada control con el mismo `id`**. Dos
elementos con el mismo `id` rompen la relación `label`/campo, así que pulsar la etiqueta habría llevado
el foco al campo equivocado y un lector de pantalla habría anunciado el que no es. Lo destapó que los
tests del diario empezaron a encontrar elementos duplicados.

La corrección se reparte según **qué** cambia entre móvil y escritorio:

| Qué cambia | Cómo se resuelve | Dónde |
|---|---|---|
| Solo la presentación | CSS, con variantes `[&>*]` sobre el contenedor | `SummaryStrip` |
| La estructura | Se **elige** un envoltorio con una media query | `MobileDisclosure` |

`MobileDisclosure` usa `<details>` nativo: trae de serie el plegado accesible —teclado, estado
expuesto y anuncio en lector de pantalla— sin escribir nada de eso.

### Por qué el resumen no se pliega y los filtros sí

Responden a intenciones distintas: el resumen se mira **al entrar**, y a los filtros se va cuando ya se
sabe qué se quiere acotar. Esconder el resumen detrás de una pulsación lo convertiría en algo que nadie
abre.

Y el peor efecto de esconder filtros —no saber que están puestos— lo cierra `activeCount`: con el panel
cerrado, un número dice cuántos acotan lo que se está viendo, y si hay alguno el panel **arranca
abierto**.

### El alta de compra, plegada en móvil

No estaba en la letra del alcance y sin ella el `CA-4` no se cumple en Compras. Es el **único**
formulario del producto que vive en línea en vez de en un modal —Diario y Cosechas abren el suyo desde
un botón— y sus ~335 px dejaban la primera fila del libro en `y=802` con la pantalla de 812: visible
sobre el papel, invisible en la práctica. Plegado, se comporta como los otros dos. No se rediseña el
formulario, solo su contenedor, que es la frontera que marca el spec.

## Alternativas descartadas

| Alternativa | Por qué no |
|---|---|
| Retirar el contenedor y usar todo el ancho | A 1920 una tabla de 8 columnas queda desierta, y se pierde la coherencia de `P-016` |
| Una sola cota más ancha para todo | Campos de texto de 1.000 px y párrafos ilegibles en Ajustes (CA-2) |
| Que cada vista declare su ancho | La coherencia duraría hasta la siguiente vista que se añadiera |
| Seguir con `lg:` y ensanchar el contenedor | Arregla el síntoma a 1920 y deja la pregunta equivocada para el próximo cambio de cota |
| Pintar móvil y escritorio y ocultar uno | Duplica los `id` de los controles y rompe `label`/campo |
| Mover el corte del lateral a `lg` para ganar sitio a 768 | El spec deja fuera cambiar la navegación lateral |

## Riesgos e impacto

- **Cambio visible en todas las pantallas del área operativa.** Es el objetivo.
- `matchMedia` no existe en jsdom: se añade a la preparación común de los tests, declarando escritorio
  por defecto, que es como están escritas las comprobaciones existentes.
- **A 768 px exactos una tabla ancha sigue pidiendo desplazamiento horizontal dentro de su tarjeta.**
  Es **anterior a esta historia** y no lo introduce: medido en `develop`, el contenedor es 448 px y la
  tabla 917, exactamente igual que aquí. La causa es que a 768 aparece el lateral y se lleva 256 px;
  corregirlo exige mover su punto de corte, que el spec deja fuera. Queda registrado.

## Plan de testing

| Nivel | Qué cubre |
|---|---|
| Unitario (`MobileDisclosure.test.tsx`) | **Que los hijos se rendericen una sola vez** en móvil y en escritorio; que en escritorio no haya desplegable; que en móvil baste una acción; que arranque abierto y con el número si ya hay filtros |
| Unitario (resto) | Los 23 tests del diario y los del panel siguen pasando: son la red de que la reorganización no cambia comportamiento |
| UI conducida | Medidas reales a 375, 768, 1280 y 1920 px |

## Verificación realizada

Sobre la aplicación en marcha. Las medidas se toman de `document.documentElement.clientWidth`, no de
`window.innerWidth`: en el panel de pruebas `innerWidth` informa del tamaño **exterior** y da números
falsos —llegó a decir 805 con el motor de CSS a 375—.

**Escritorio (1920x1000 CSS):**

| Comprobación | Resultado |
|---|---|
| Contenedor de Cosechas | **1280 px** (antes 768) |
| Tabla de Cosechas | 1278 px, **sin desplazamiento horizontal** |
| Contenedor de Terrenos | 1280 px — mismo tipo, misma medida (CA-3) |
| Resumen del panel | **4 columnas**, tarjetas de 308 px |
| Contenedor de Ajustes | **768 px**, campo de texto de 718 px (CA-2) |

**Móvil (375x812 CSS):**

| Vista | Resumen | Filtros | Primera fila | Holgura |
|---|---|---|---|---|
| Cosechas | tira de 95 px | plegados | y=530 | 282 px |
| Diario | tira de 95 px | plegados | y=291 | 521 px |
| Compras | — | plegados | **y=497** | 315 px |

En Compras, antes de plegar el alta la primera fila caía en `y=802` sobre 812 de alto. Después, en 497.

**1280x800**: contenedor 945 px, 3 columnas, filtros a la vista, sin desplegable.

**768x1024**: contenedor 448 px y tabla 917 → desplazamiento horizontal dentro de la tarjeta.
Comprobado en `develop`: **448 y 917 también**, así que es anterior y no lo introduce esta historia.

**Lo que no he podido aportar**: el `CA-6` pide **captura de cada ancho** y el panel del navegador no
se puede mostrar en este entorno, así que las capturas no existen. Lo que sí hay son las medidas de
arriba, que para «cuántas columnas» y «hay desplazamiento horizontal» son evidencia más fuerte que una
imagen. El criterio queda `[~]`.

## Checklist de implementación

- [x] Dos cotas de ancho por tipo de contenido, decididas en un único sitio
- [x] Rejilla del panel por consulta de contenedor
- [x] Resumen en fila desplazable y filtros plegados en móvil, en las tres vistas
- [x] Los envoltorios renderizan los hijos **una sola vez**
- [x] 167 tests de frontend en verde
- [~] Capturas de los cuatro anchos: sustituidas por medidas; el panel no es mostrable aquí
