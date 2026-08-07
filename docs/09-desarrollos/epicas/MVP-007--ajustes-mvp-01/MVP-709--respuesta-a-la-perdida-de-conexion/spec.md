---
id: "MVP-709"
tipo: feature
titulo: "Respuesta a la perdida de conexion"
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
  dominios: ["ux", "frontend", "resiliencia"]
  modulo_path: "03-modulos/"
  componentes: ["http-client", "formularios", "avisos"]
  etiquetas: ["mvp", "ajustes", "ux", "campo"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
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

- [ ] **CA-1**: Con la red cortada, la aplicacion dice «sin conexion» y no «no se pudieron cargar los
  datos», que es lo mismo que dice ante un fallo del servidor.
- [ ] **CA-2**: Al recuperar la conexion, el aviso desaparece sin exigir recargar.
- [ ] **CA-3**: Un guardado que falla por red conserva el contenido del formulario y ofrece reintentar.
- [ ] **CA-4**: `ADR-0002` sigue vigente: esta historia no introduce operacion sin conexion, y asi se
  hace constar.
- [ ] **CA-5**: Verificado cortando la red de verdad con el formulario abierto, no simulando el error.

## Maquetas y referencias visuales

- Referencia de flujo: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| App (transversal) | ADR-0002 (online-first) | falta | Ninguna reaccion a la perdida de red |

## Notas y decisiones

- La frontera esta escrita a proposito: **avisar y no perder** cabe en unos ajustes; **operar sin
  conexion** no, y confundirlos convertiria esta epica en otra cosa.
