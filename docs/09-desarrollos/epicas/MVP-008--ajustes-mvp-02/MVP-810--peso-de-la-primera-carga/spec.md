---
id: "MVP-810"
tipo: mejora
titulo: "Peso de la primera carga"
estado: completado
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
actualizado_en: "2026-08-11"
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

- [x] **CA-1**: El peso total de `dist/assets` baja de los **5,57 MB** medidos, con la cifra antes y
  despues y el desglose por tipo de recurso.

  **Evidencia.** Medido con el mismo instrumento (`scripts/peso-primera-carga.mjs`) sobre los dos
  builds, el 2026-08-11. `dist/assets`: **5.593.082 B -> 1.224.033 B** (5.593,1 kB -> 1.224,0 kB,
  **-78,1 %**). Primera carga —lo que el navegador descarga de verdad—: **4.591.425 B -> 877.868 B**
  (**-80,9 %**). El total de partida son los 5,57 MB que registro `P-115` mas los 21,5 kB de
  JavaScript que han anadido las siete historias hermanas de la epica mientras tanto: medido sobre el mismo
  `origin/develop` sobre el que se apoya esta rama. Desglose:

  | Recurso | Antes | Despues |
  |---|---:|---:|
  | Documento (`index.html`) | 4,4 kB | 4,4 kB |
  | JavaScript | 591,5 kB | 591,5 kB |
  | CSS | 74,1 kB | 62,3 kB |
  | Iconos (Material Symbols) | 3.776,0 kB | 74,2 kB |
  | Tipografias de texto | 145,4 kB | 145,4 kB |
  | **Primera carga** | **4.591,4 kB** | **877,9 kB** |
  | Tipografias que el navegador no pide | 1.006,0 kB | 350,5 kB |
  | **Total `dist/assets`** | **5.593,1 kB** | **1.224,0 kB** |

- [x] **CA-2**: Ninguna pantalla pierde un icono ni lo cambia por otro. Verificado recorriendo las
  vistas operativas y los maestros, no solo el shell.

  **Evidencia.** El inventario sale de **los 189 `<span>` de iconos de todo el cliente** —shell,
  vistas operativas, maestros, onboarding, portada y paginas legales— mas las propiedades `icon` que
  los alimentan: **75 nombres**, listados en el `tech-design.md`. Que el subconjunto los cubre no se
  supone: para cada nombre se compara el **contorno** del glifo entre la fuente completa y la
  recortada, en las dos instancias que declara el CSS (`FILL 0` y `FILL 1`).

  ```text
  Iconos: 75  Glifos en el subconjunto: 154
    = 75 base + 53 variantes rellenas + 26 letras
  Glifos comparados: 75 x 2 variantes = 150
  IDENTICOS: sin diferencias
  ```

  La comparacion vive dentro del generador, asi que se repite en cada `build`. Encontro un fallo
  real: la primera version del recorte perdia las **53 variantes rellenas** (`rvrn`), lo que habria
  dejado el distintivo `eco` de la marca sin rellenar en cinco pantallas sin que el peso ni la
  composicion lo delataran.

- [x] **CA-3**: Ningun recurso se sirve desde un host externo: la prueba que ya existe contra recursos
  externos sigue en verde.

  **Evidencia.** `src/test/sin-recursos-externos.test.ts` (de `MVP-599`), **2 tests en verde**. Todo
  sigue autoalojado: el subconjunto se genera desde `node_modules` y se sirve desde el propio
  origen, y la CSP no cambia (`font-src 'self'`). `RN-042` intacta.

- [x] **CA-4**: El `build` falla si el peso de la primera carga supera el umbral acordado, comprobado
  **provocando el fallo**.

  **Evidencia.** Umbrales fijados con la medida delante: **1.000 kB** de primera carga (13,9 % de
  margen sobre los 877,9 kB medidos) y **1.400 kB** de `dist/assets` (14,4 % sobre 1.224,0 kB). Para
  provocar el fallo se volvio a importar la fuente completa —la regresion exacta que el presupuesto
  vigila—, se ejecuto `npm run build` y se restauro despues. Salida literal:

  ```text
  [MVP-810] Peso de la primera carga
    Documento (index.html)                                4.4 kB
    JavaScript                                          591.5 kB
    CSS                                                  62.9 kB
    Iconos (Material Symbols)                          3850.2 kB
    Tipografías de texto                                145.4 kB
    PRIMERA CARGA                                      4654.4 kB

    Tipografías no pedidas (otros alfabetos y reserva .woff)    350.5 kB
    TOTAL dist/assets                                  5000.6 kB
    Copiado de public/ (fuera del presupuesto)          231.1 kB

  error during build:
  Build failed with 1 error:

  [plugin terrenario-presupuesto-primera-carga]
  RolldownError: El peso de la primera carga se ha pasado del presupuesto (MVP-810):
    - Primera carga: 4654.4 kB supera el umbral de 1000.0 kB (sobran 3654.4 kB). Es lo que
      descarga alguien que abre la aplicación por primera vez, y el usuario objetivo trabaja con
      mala cobertura (RT-01, MVP-709).
    - Total de dist/assets: 5000.6 kB supera el umbral de 1400.0 kB (sobran 3600.6 kB). Aunque no
      todo se descargue en la primera visita, es lo que hay que publicar y mantener.

  Si el aumento está justificado, sube el umbral en `scripts/peso-primera-carga.mjs` y explica en
  el PR por qué. Lo que no vale es subirlo sin decirlo.
  ```

  Codigo de salida `1` comprobado; tras restaurar, `0`.

- [x] **CA-5**: El proceso de generacion del subconjunto es reproducible y esta documentado: quien
  anada un icono nuevo tiene que saber que hacer para que aparezca.

  **Evidencia.** Reproducible: lo genera el `build` (`vite build` y `vite dev`) desde el codigo y
  `node_modules`, sin pasos manuales y sin artefactos versionados; es idempotente y tarda 1,9 s la
  primera vez. Documentado en tres sitios: **`docs/04-ingenieria/estandares-codigo.md`** (como se
  escribe un icono, que guarda lo detecta y cuando), **`docs/05-infraestructura/desarrollo-local.md`**
  (que ficheros genera el arranque y que hay que reiniciar el servidor de desarrollo) y la cabecera
  de **`scripts/inventario-iconos.mjs`**, que empieza con «SI VIENES A ANADIR UN ICONO NUEVO».

  Y no depende de que se lea. Las dos guardas, comprobadas provocandolas:

  ```text
  # Un nombre que no existe en la fuente -> el build falla en buildStart
  [plugin terrenario-tipografias-generadas]
  Error: Estos nombres no son iconos de Material Symbols Outlined: icono_que_no_existe.
  Compruébalos en https://fonts.google.com/icons (estilo Outlined). Si no es un icono, no lo
  llames `icon` ni lo pongas dentro de un `<span class="material-symbols-outlined">`.
  ```

  ```text
  # Un <span> cuyo nombre no se puede leer en el codigo -> falla el test
  FAIL src/test/inventario-iconos.test.ts > todo `<span>` de iconos dice qué glifo pinta
  AssertionError: El build solo empaqueta los glifos que encuentra en el código. Escribe el
  nombre como cadena literal dentro del `<span>`, o pásalo por una propiedad `icon` (ver la
  cabecera de `scripts/inventario-iconos.mjs`). Si no, el icono no se descarga y sale un hueco.
  + [ "src/components/TmpPruebaIcono.tsx:2 → {nombre}" ]
  ```

## Notas y decisiones

- **`CA-5` es lo que evita que este arreglo se convierta en una trampa.** Un subconjunto silencioso
  hace que el proximo icono que alguien use simplemente no se pinte, y el sintoma —un cuadro vacio— no
  apunta a la causa.
- **El umbral se acuerda al implementar**, con la medida real delante. Fijarlo aqui sin saber cuanto
  baja el subconjunto seria inventarse un numero.
- **Se eligio el subconjunto, no el SVG en linea.** La premisa del alcance no se sostenia:
  `public/icons.svg` no es iconografia del producto, sino seis marcas de terceros heredadas de una
  plantilla que **no referencia ni una linea de codigo**. Aparte de eso, el SVG obligaria a extraer
  128 contornos (75 normales y 53 rellenos), tocar los 189 puntos de uso y reproducir a mano el
  tamano y la alineacion que hoy da la tipografia: mas riesgo de cambiar el aspecto —lo que `CA-2`
  prohibe— a cambio de unos pocos kB frente a los 74,2 del subconjunto. Razonado en el
  `tech-design.md` junto al resto de alternativas descartadas.
- **Las tipografias de texto ya estaban bien, y la cifra de `P-115` mezclaba dos cosas.** De los
  4,93 MB de tipografias que se publicaban, la primera carga solo pedia **145,4 kB**: el
  `unicode-range` de `@fontsource` impide que se pidan los alfabetos que el producto no usa. Se
  conservan por eso mismo —cuestan cero descargas y dan cobertura—. Lo que si sobraba era la copia
  **`.woff` de reserva**, 655,5 kB que duplican bytes que ya estan en `woff2` para navegadores que no
  pueden ejecutar esta aplicacion: se quita, y el CSS adelgaza 11,9 kB de paso.
- **Comprobacion visual pendiente del PO.** El cambio no toca ningun componente, pero conviene mirar
  las pantallas donde aparece el distintivo `eco` relleno (lateral, login, portada, inicio y alta de
  temporada) y una vista operativa cualquiera con sus iconos de estado.
