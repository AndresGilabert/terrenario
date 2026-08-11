---
id: "MVP-899"
tipo: feature
titulo: "Revision epica"
estado: completado
prioridad: media
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: ["MVP-801", "MVP-802", "MVP-803", "MVP-804", "MVP-805", "MVP-806", "MVP-807", "MVP-808", "MVP-809", "MVP-810", "MVP-811"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["calidad", "gobernanza"]
  modulo_path: "03-modulos/"
  componentes: ["revision", "verificacion"]
  etiquetas: ["mvp", "ajustes", "revision"]
  nivel_riesgo: medio
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-899 — Revision epica

## Contexto

Revision de cierre de la epica, con la misma mecanica que `MVP-199`, `MVP-299`, `MVP-399`, `MVP-499`,
`MVP-599`, `MVP-699` y `MVP-799`: pasada de verificacion **real** contra el flujo integrado —no
relectura de la KB— para producir hallazgos con evidencia, veredicto por cada criterio de aceptacion de
la epica y derivacion de lo que no se corrija.

Esta revision tiene dos encargos propios, heredados de lo que la segunda revision del MVP descubrio:

- **Comprobar que las guardas nuevas fallan de verdad.** `MVP-809` anade una comprobacion al gate de KB
  y `MVP-810` un limite de peso al `build`. Las dos son reglas, y la leccion de `P-096` es que una
  regla que nadie provoca es una regla que nadie comprueba.
- **Medir, no releer.** Los tres hallazgos con filo de la revision de agosto salieron de contar y
  comparar cifras del sistema en marcha: el ambito que devuelve un endpoint frente al que devuelve otro
  con la misma entrada, cuantos requisitos se citan fuera de su documento, y cuanto pesa de verdad la
  primera carga.

## Objetivo

Dar por cerrada la epica solo si lo que dice estar hecho lo esta, medido contra el sistema en marcha.

## Requisitos de usuario

### HU-1 — Cerrar sobre evidencia

**Como** Product Owner,
**quiero** que el cierre de la epica se sostenga en verificacion real,
**para** no repetir el patron de dar por resuelto lo que solo estaba anotado.

## Alcance (in-scope)

- Verificacion conducida de los seis criterios de aceptacion de la epica, con tabla de veredicto y
  evidencia por cada uno.
- **Contraste del escenario que origino la epica**: un `season_id` y unos `plot_ids` de otro Workspace
  en la URL de las cuatro vistas operativas, comprobando que ninguna se vacia y que ninguna afirma un
  ambito distinto del que aplica.
- **Provocacion de las dos guardas nuevas**: el gate de KB ante un requisito MVP sin destino, y el
  `build` ante un exceso de peso.
- Comprobacion de que los 18 puntos con destino `MVP-008` tienen historia que los recoge y evidencia de
  cierre, y de que **ninguna fila del registro sigue diciendo `triado`** con el trabajo hecho.
- Correcciones de cierre acotadas en la propia rama de revision, segun el criterio ya establecido en el
  proyecto: lo pequeno se arregla aqui, lo que necesita decision de producto sale a `MVP-999`.
- Actualizacion de `RN-007`, `RN-008`, `RN-034` y `RN-037`, si alguna historia las dejo a medias.

## Fuera de alcance (out-of-scope)

- Implementar funcionalidad nueva.
- Reabrir las decisiones tomadas en el triaje del 2026-08-10, incluidos los 18 puntos que se quedaron
  en backlog.

## Criterios de aceptación

- [x] **CA-1**: Tabla de veredicto con evidencia por cada criterio de aceptacion de `MVP-008`, medida
  contra el sistema en marcha y no releida.
  **Evidencia**: la tabla esta en el `tech-design.md`, con los seis criterios en **CUMPLE**. Cada uno
  lleva la cifra o la salida que lo sostiene, obtenida de la API real contra la base de datos de
  desarrollo y del navegador conducido.
- [x] **CA-2**: Hallazgos numerados `R-xx` con evidencia reproducible, dados de alta como `P-xxx` en
  `MVP-999` con destino explicito.
  **Evidencia**: cinco hallazgos, dados de alta como **`P-124`** a **`P-128`**. Cuatro salieron del
  propio desarrollo —la purga de retencion frente a la autoria, los filtros del diario con la forma de
  `P-108`, la escritura ciega de `TerrenosView` y la bandeja inalcanzable sin Workspace— y el quinto es
  una nota de coste sin accion propuesta. Ninguno se arregla aqui: los cinco tienen destino escrito.
  A ellos se suma **`P-123`**, dado de alta durante `MVP-807`.
  **Dos de ellos ya tienen decision del PO (2026-08-11)**, tomada dentro de esta misma pasada:
  - **`P-123`** — el producto **si quiere copropiedad**. Lo que falta no son las guardas, que ya estan
    escritas para ese estado, sino la **accion de promover a un miembro sin degradar a nadie**. Pasa a
    backlog: no hay sintoma —nadie puede alcanzar el estado incoherente— y construirla pide superficie
    propia y decidir quien puede promover. Recogido en la nota de estado de `RN-034`.
  - **`P-124`** — se **acepta perder la autoria** pasados los 24 meses: lo que interesa conservar es el
    historico de datos, no quien lo tecleo hace mas de dos anos. **No hay cambio de codigo**; lo que
    habia que arreglar era lo que la KB prometia, asi que se cierra **aqui** como decision documental:
    `RN-041` recoge la consecuencia y dice que su frase sobre la fila anonimizada describe los primeros
    24 meses y no siempre, y `CloseAccountHandler` lo precisa donde vive esa razon. Se descarta retener
    la cuenta mientras le quede historico: ninguna cuenta que haya trabajado se purgaria nunca y el
    plazo de `RN-041` pasaria a ser una promesa incumplida, que es el mismo problema al reves.
- [x] **CA-3**: Los 18 puntos con destino `MVP-008` estan cerrados con la evidencia de lo que se
  construyo, y ninguno se queda en `triado`.
  **Evidencia**: los 18 en `resuelto`, repartidos entre las once historias. `P-111` sigue en
  `backlog-post-mvp` **a proposito** —la epica solo prometia corregir el estado de `RU-32`/`RU-33`/
  `RU-34`, no construir la planificacion— y `P-069` ya estaba `resuelto` desde el gate de `MVP-504`.
  Recuento del registro entero: 128 puntos, 99 resueltos, 18 en backlog, 10 pendientes y 1 descartado.
- [x] **CA-4**: Las dos guardas nuevas se han **provocado**, no leido: se aporta la salida del fallo.
  **Evidencia**: en el `tech-design.md`, la salida literal de las dos. El gate de KB se hizo fallar con
  un `RU-99` marcado MVP **de las dos formas** en que se puede no tener destino —sin fila en la matriz
  y con la celda vacia—, y el `build` reintroduciendo **la regresion exacta que vigila**: volver a
  importar la fuente de iconos entera, que lo lleva de 881,1 kB a 4.657,6 kB. Las dos sondas se
  retiraron y las dos comprobaciones vuelven a verde.
- [x] **CA-5**: `RN-007`, `RN-008`, `RN-034` y `RN-037` reflejan el estado real del producto,
  comprobadas contra el sistema y no solo contra su redaccion.
  **Evidencia**: `RN-007` y `RN-008`, en las cuatro vistas con un `season_id` ajeno (tabla del
  `tech-design.md`). `RN-037`, borrando un maestro **con** uso —`422` con «1 actividad y 3 cosechas lo
  referencian»— y otro **sin** uso, creado y borrado en la pasada, restaurando el estado. `RN-034`,
  intentando abandonar siendo propietario unico (`422`) y comprobando que `can_revoke` de la unica
  miembro es `false`, es decir, que coincide con la guarda.
  **Con una salvedad que la propia revision destapo**: la nota de estado de `RN-034` sobre la
  copropiedad es cierta —ningun flujo produce dos propietarios activos— y por eso el `CA-6` de
  `MVP-807` hubo que comprobarlo sembrando el estado en base de datos. Queda como `P-123`.

## Maquetas y referencias visuales

No aplica.

## Notas y decisiones

- **El contraste hay que provocarlo.** `MVP-799` dejo escrita la leccion: con los datos que habia, el
  contraste numerico habria pasado limpio sin tocar nada de lo que la epica construyo. Hay que crear el
  caso que si puede divergir, medirlo y despues restaurar el estado anterior.
- **Re-medir lo que reporten otros, no citarlo.** En `MVP-799` tres hallazgos resultaron bastante
  mayores al comprobarlos. El informe da la pista; la cifra hay que sacarla uno mismo.
