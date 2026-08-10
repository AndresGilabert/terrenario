---
id: "MVP-810"
tipo: mejora
titulo: "Peso de la primera carga"
estado: aprobado
prioridad: media
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["rendimiento", "frontend", "ux"]
  modulo_path: "03-modulos/"
  componentes: ["plataforma-de-aplicacion", "shell"]
  etiquetas: ["mvp", "ajustes", "rendimiento", "movil"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-810 — Peso de la primera carga

> **Origen**: `P-115` del registro de `MVP-999`, detectado en la segunda revision completa del MVP
> (2026-08-10).

## Contexto

Medido sobre el `build` de produccion: `dist/assets` suma **5,57 MB**, de los que **4,27 MB son
tipografias** y **3,78 MB un unico fichero**, `material-symbols-outlined.woff2`. Todo el JavaScript y
el CSS de la aplicacion suman **643 kB**. Es decir: **los iconos pesan casi seis veces la aplicacion
entera**.

Se descarga completo en la primera visita —confirmado en la pestana de red— porque es la fuente
variable con el catalogo completo de Material Symbols, del que el producto usa unas decenas de glifos.

La autoalojacion es correcta y no se discute: viene de `RN-042`, que decidio no transferir la IP de
cada visitante al CDN de Google, y de `P-008`, que la resolvio asi en vez de con un banner de cookies.
Lo que nunca se decidio fue el **subconjunto**: al autoalojar se copio el fichero entero.

Importa por quien usa esto. `RT-01` exige que la experiencia funcione bien en movil, `MVP-709` existe
justamente porque estas personas trabajan con cobertura mala, y la primera pantalla que ve alguien a
quien acaban de invitar es la que decide si vuelve.

## Objetivo

Que la primera carga deje de estar dominada por iconos que no se usan, sin renunciar a la
autoalojacion ni cambiar el aspecto de ninguna pantalla.

## Requisitos de usuario

### HU-1 — Entrar rapido desde el campo

**Como** persona que abre la aplicacion por primera vez desde el movil, con mala cobertura,
**quiero** que la aplicacion cargue sin descargar varios megabytes,
**para** poder empezar a usarla.

## Alcance (in-scope)

- Inventario de los glifos que el producto usa de verdad, obtenido del codigo y no estimado.
- **Subconjunto** de la fuente limitado a esos glifos, generado de forma reproducible en el `build`, o
  sustitucion por SVG en linea aprovechando que el proyecto ya tiene `public/icons.svg`. Se elige lo
  que deje menor peso sin cambiar el aspecto.
- Revision del mismo criterio en las tipografias de texto (`Inter`, `Plus Jakarta Sans`): comprobar que
  no se sirven variantes que nadie usa.
- **Prueba automatica que fije el limite**: el `build` falla si el peso de los recursos de primera
  carga vuelve a superar el umbral acordado. Sin eso, el peso vuelve en la siguiente dependencia.
- Medida antes y despues en el `spec` de cierre, con el desglose por tipo de recurso.

## Fuera de alcance (out-of-scope)

- **Volver a servir las tipografias desde un CDN**: `RN-042` lo prohibe y esta historia no lo reabre.
- Cambiar el sistema de iconografia visible: los mismos iconos, con el mismo aspecto.
- Optimizacion del JavaScript (division en trozos, carga diferida de rutas): los 643 kB no son el
  problema medido.
- Politica de cache o service worker.

## Criterios de aceptación

- [ ] **CA-1**: El peso total de `dist/assets` baja de los **5,57 MB** medidos, con la cifra antes y
  despues y el desglose por tipo de recurso.
- [ ] **CA-2**: Ninguna pantalla pierde un icono ni lo cambia por otro. Verificado recorriendo las
  vistas operativas y los maestros, no solo el shell.
- [ ] **CA-3**: Ningun recurso se sirve desde un host externo: la prueba que ya existe contra recursos
  externos sigue en verde.
- [ ] **CA-4**: El `build` falla si el peso de la primera carga supera el umbral acordado, comprobado
  **provocando el fallo**.
- [ ] **CA-5**: El proceso de generacion del subconjunto es reproducible y esta documentado: quien
  anada un icono nuevo tiene que saber que hacer para que aparezca.

## Notas y decisiones

- **`CA-5` es lo que evita que este arreglo se convierta en una trampa.** Un subconjunto silencioso
  hace que el proximo icono que alguien use simplemente no se pinte, y el sintoma —un cuadro vacio— no
  apunta a la causa.
- **El umbral se acuerda al implementar**, con la medida real delante. Fijarlo aqui sin saber cuanto
  baja el subconjunto seria inventarse un numero.
