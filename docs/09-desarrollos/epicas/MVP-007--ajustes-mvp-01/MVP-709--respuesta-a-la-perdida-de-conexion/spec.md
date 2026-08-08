---
id: "MVP-709"
tipo: feature
titulo: "Respuesta a la perdida de conexion"
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
  dominios: ["ux", "frontend", "resiliencia"]
  modulo_path: "03-modulos/"
  componentes: ["http-client", "formularios", "avisos"]
  etiquetas: ["mvp", "ajustes", "ux", "campo"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-709 — Respuesta a la perdida de conexion

> **Origen**: `P-091` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

El producto no reacciona de ninguna forma a la perdida de conectividad. Confirmado por busqueda: cero
referencias a `navigator.onLine`, sin service worker y sin ninguna cola de reintento. `ADR-0002` decide
«online-first» y esto es coherente con esa decision, asi que **no es un defecto**.

Pero el usuario objetivo trabaja en campo, donde la cobertura falla. Hoy un corte se traduce en un «No
se pudieron cargar…» generico —indistinguible de un fallo del servidor— y, si estaba escribiendo una
labor, en perder lo escrito al fallar el guardado.

## Objetivo

Que una caida de red se reconozca como tal, se diga con claridad y no cueste el trabajo ya tecleado.

## Requisitos de usuario

### HU-1 — Saber que el problema es la cobertura

**Como** titular de la explotacion en el campo,
**quiero** que se me diga que no hay conexion,
**para** no pensar que la aplicacion se ha roto ni volver a intentarlo a ciegas.

### HU-2 — No perder lo que acabo de escribir

**Como** persona registrando una labor,
**quiero** que si falla el guardado por red se conserve lo que habia escrito,
**para** poder reintentarlo cuando vuelva la cobertura en vez de teclearlo otra vez.

## Alcance (in-scope)

- Deteccion de la caida en el cliente HTTP comun: distinguir el fallo de red del error de respuesta del
  servidor.
- Aviso claro y persistente mientras no haya conexion, y su retirada al volver.
- Conservacion del contenido del formulario cuando el guardado falla por red, con reintento explicito.

## Fuera de alcance (out-of-scope)

- **Offline real**: outbox, idempotencia, reintentos automaticos y resolucion de conflictos. Eso es
  `Hito H — Resiliencia offline` y una reescritura del modelo de datos del cliente.
- Service worker, cache de aplicacion o instalabilidad, que es `MVP-710` en su parte de manifest.
- Registrar operaciones sin conexion.

## Criterios de aceptación

- [x] **CA-1**: Con la red cortada, la aplicacion dice «sin conexion» y no «no se pudieron cargar los
  datos». La deteccion vive en un unico sitio —`fetch` solo rechaza cuando la peticion **no llega a
  tener respuesta**, que es justo la frontera que separa la falta de cobertura del error del servidor—
  y el `NetworkError` **hereda de `HttpError`** a proposito: media aplicacion esta escrita como
  `error instanceof HttpError ? error.message : generico`, asi que heredando el mensaje correcto sale
  en todas las pantallas a la vez en lugar de tener que repasarlas una a una.
- [x] **CA-2**: Al recuperar la conexion, el aviso desaparece sin exigir recargar. Lo retiran dos
  fuentes: el evento `online` del navegador y la primera peticion que vuelve con respuesta —aunque sea
  un 500: eso ya demuestra que hay conexion—. Comprobado en UI conducida con **una sola navegacion**
  registrada: el aviso se fue solo.
- [x] **CA-3**: Un guardado que falla por red conserva el contenido del formulario y ofrece reintentar.
  Verificado con 1847 kg escritos: el modal sigue abierto, el valor sigue ahi y «Guardar» vuelve a
  estar disponible; al volver la API, el mismo boton lo guardo sin volver a teclear nada.
- [x] **CA-4**: `ADR-0002` sigue vigente. No hay cola de salida, ni reintento automatico de
  operaciones, ni almacenamiento local de nada que el usuario haya escrito, ni resolucion de
  conflictos. Solo se **sabe** si hay conexion para poder decirlo. El unico reintento automatico es el
  del refresco de sesion, que no es una operacion del usuario y no crea ni modifica datos.
- [x] **CA-5**: Verificado cortando la red de verdad —**parando el proceso de la API**, no simulando el
  error— con el formulario de cosecha abierto y relleno. Ademas, la prueba de regresion de la sesion se
  comprobo rompiendola a proposito para confirmar que falla sin el arreglo.

## Maquetas y referencias visuales

- Referencia de flujo: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| App (transversal) | ADR-0002 (online-first) | hecho | Corte real de la conexion con el formulario abierto: aviso persistente, formulario conservado y reintento correcto sin recargar |

## Notas y decisiones

- La frontera esta escrita a proposito: **avisar y no perder** cabe en unos ajustes; **operar sin
  conexion** no, y confundirlos convertiria esta epica en otra cosa.
- **Hallazgo: un corte de cobertura cerraba la sesion.** Al recorrer el camino de red aparecio algo
  peor de lo que el punto describia: habia **tres** sitios donde una red caida se leia como sesion
  invalida —el refresco programado, que se dispara solo cada cuarto de hora largo; `getAccessToken`,
  que devolvia `null` y el cliente traducia a «cerrar sesion»; y el arranque con token guardado, que lo
  borraba—. Cerrar la sesion se lleva por delante todo lo tecleado, que es exactamente lo que el `HU-2`
  quiere evitar, asi que entra en el alcance. Ahora solo se cierra cuando el servidor **responde** que
  la sesion no vale; si fue la red, se conserva y se reintenta. Es seguro porque el `refresh_token` es
  una cookie de larga duracion que sigue valiendo cuando vuelva la cobertura.
- **`navigator.onLine` no basta y no se usa solo.** Sabe si hay interfaz de red, no si se llega al
  servidor, y en el campo el caso normal es el contrario del que detecta: el movil enganchado a una
  antena con una barra, `navigator.onLine` en `true` y las peticiones muriendo igual. Su `false` es
  fiable; su `true` no significa nada. Por eso manda lo que le pasa a cada peticion.
- **Cancelar no es un corte.** Un `AbortError` se deja pasar tal cual: abortar al cambiar de pantalla es
  lo normal y confundirlo con falta de cobertura pintaria el aviso cada vez que alguien navega rapido.
- **Arrancar con un token guardado no programa el refresco de sesion.** Se descubrio al montar la
  prueba de regresion, que sin darse cuenta no llegaba a disparar el temporizador. Queda **fuera de
  alcance** —no lo empeora esta historia y arreglarlo toca el ciclo de sesion— y se registra en
  `MVP-999`.
