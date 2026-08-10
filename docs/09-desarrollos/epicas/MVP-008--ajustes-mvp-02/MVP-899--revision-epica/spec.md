---
id: "MVP-899"
tipo: feature
titulo: "Revision epica"
estado: aprobado
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

- [ ] **CA-1**: Tabla de veredicto con evidencia por cada criterio de aceptacion de `MVP-008`, medida
  contra el sistema en marcha y no releida.
- [ ] **CA-2**: Hallazgos numerados `R-xx` con evidencia reproducible, dados de alta como `P-xxx` en
  `MVP-999` con destino explicito.
- [ ] **CA-3**: Los 18 puntos con destino `MVP-008` estan cerrados con la evidencia de lo que se
  construyo, y ninguno se queda en `triado`.
- [ ] **CA-4**: Las dos guardas nuevas se han **provocado**, no leido: se aporta la salida del fallo.
- [ ] **CA-5**: `RN-007`, `RN-008`, `RN-034` y `RN-037` reflejan el estado real del producto,
  comprobadas contra el sistema y no solo contra su redaccion.

## Maquetas y referencias visuales

No aplica.

## Notas y decisiones

- **El contraste hay que provocarlo.** `MVP-799` dejo escrita la leccion: con los datos que habia, el
  contraste numerico habria pasado limpio sin tocar nada de lo que la epica construyo. Hay que crear el
  caso que si puede divergir, medirlo y despues restaurar el estado anterior.
- **Re-medir lo que reporten otros, no citarlo.** En `MVP-799` tres hallazgos resultaron bastante
  mayores al comprobarlos. El informe da la pista; la cifra hay que sacarla uno mismo.
