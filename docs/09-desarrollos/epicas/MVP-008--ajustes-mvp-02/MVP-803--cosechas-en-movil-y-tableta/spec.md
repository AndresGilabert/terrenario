---
id: "MVP-803"
tipo: mejora
titulo: "Cosechas en movil y tableta"
estado: aprobado
prioridad: media
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: []
bloquea: ["MVP-805"]
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend", "accesibilidad"]
  modulo_path: "03-modulos/"
  componentes: ["produccion", "shell"]
  etiquetas: ["mvp", "ajustes", "responsive", "movil"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-803 — Cosechas en movil y tableta

> **Origen**: `P-095` del registro de `MVP-999`, abierto en `MVP-702` y ampliado en la segunda revision
> del MVP (2026-08-10).

## Contexto

`P-095` se registro como un problema del punto de corte del lateral: a 768 px exactos el lateral
aparece justo en ese ancho y se lleva 256, de modo que al contenido le quedan **448** —menos que en
movil mas un poco— mientras la tabla de Cosechas necesita **917**. En tableta en vertical se leen tres
columnas de ocho. Se midio tambien en `develop`, asi que es anterior a `MVP-702` y esa historia no lo
introdujo.

La segunda revision anadio la medida que cambia el diagnostico: **a 375 px la misma tabla mide 891 px
dentro de un contenedor de 341**, y ahi el lateral ni siquiera esta. Mover el punto de corte no arregla
eso. El problema de fondo es otro: **Cosechas es la unica lista operativa sin maqueta adaptada**. El
diario, las compras y los maestros pasan a tarjetas en pantallas estrechas; Cosechas mantiene la tabla
de ocho columnas y la mete en un contenedor con desplazamiento horizontal.

`RT-01` exige que la experiencia funcione bien en movil y en escritorio, y el perfil de usuario del
producto trabaja en el campo con el telefono.

## Objetivo

Que Cosechas se lea en movil y en tableta con el mismo criterio que el resto de listas operativas, sin
desplazamiento horizontal y sin perder informacion util.

## Requisitos de usuario

### HU-1 — Consultar las partidas desde el telefono

**Como** titular de la explotacion,
**quiero** ver mis partidas recolectadas en el movil sin arrastrar la tabla de lado a lado,
**para** poder consultarlas en el campo.

## Alcance (in-scope)

- Maqueta de **tarjeta** para Cosechas por debajo del punto de corte, coherente con la del diario y la
  de compras: fecha y campana, terreno, kilos, aceite, importe, destino y las acciones.
- **Punto de corte del lateral a `lg:`** (decision del PO, 2026-08-10): en 768–1023 px se usa el menu
  desplegable que ya existe para movil, y el contenido recupera los 256 px enteros.
- Comprobacion de los cuatro anchos que `MVP-702` uso como referencia, con la medida antes y despues.

## Fuera de alcance (out-of-scope)

- Rediseñar la navegacion lateral mas alla de mover su punto de corte. Se descarta **plegarla a
  iconos** en el tramo de tableta: conservaria la navegacion visible y daria ~192 px, pero obliga a
  resolver los rotulos al pasar el raton y las diez entradas agrupadas en tres secciones sin texto, y
  anade un tercer estado del shell que mantener.
- Cambiar las columnas, los calculos o los filtros de Cosechas: los filtros son `MVP-802`.
- Tocar las demas listas, que ya tienen maqueta adaptada.

## Criterios de aceptación

- [ ] **CA-1**: A 375 px, Cosechas no produce desplazamiento horizontal en ningun contenedor, y toda la
  informacion de cada partida es legible sin desplazar.
- [ ] **CA-2**: A 768 px el lateral ya no ocupa espacio fijo y el ancho util del contenido recupera los
  **256 px** que se llevaba, partiendo de los **448 px** medidos hoy. Se aporta la cifra medida antes y
  despues, no la estimada. El menu de navegacion sigue siendo alcanzable en ese ancho.
- [ ] **CA-3**: A 1024 px y a 1440 px la vista no cambia respecto a lo que entrego `MVP-702`.
- [ ] **CA-4**: Las acciones de corregir y eliminar una partida siguen siendo alcanzables y siguen
  nombrando la partida a la que apuntan en su etiqueta accesible.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/CosechasView.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechasView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Notas y decisiones

- **El punto se replanteo al medirlo.** Registrado como «mover el punto de corte del lateral», la
  medida a 375 px demostro que eso no lo arregla. Tratarlo como «poner Cosechas a la altura de las
  demas listas» resuelve los dos anchos de una vez.
- Va **antes de `MVP-805`**, que anade un aviso al formulario de la misma superficie.
