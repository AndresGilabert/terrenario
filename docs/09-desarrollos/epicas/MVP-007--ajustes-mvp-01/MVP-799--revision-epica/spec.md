---
id: "MVP-799"
tipo: feature
titulo: "Revision epica"
estado: completado
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: ["MVP-701", "MVP-702", "MVP-703", "MVP-704", "MVP-705", "MVP-706", "MVP-707", "MVP-708", "MVP-709", "MVP-710", "MVP-711", "MVP-712", "MVP-713", "MVP-714", "MVP-715", "MVP-716"]
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
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-799 — Revision epica

> **Origen**: — del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

Revision de cierre de la epica, con la misma mecanica que `MVP-199`, `MVP-299`, `MVP-399`, `MVP-499`,
`MVP-599` y `MVP-699`: pasada de verificacion **real** contra el flujo integrado —no relectura de la
KB— para producir hallazgos con evidencia, veredicto por cada criterio de aceptacion de la epica y
derivacion de lo que no se corrija.

Esta revision tiene ademas un encargo propio: **comprobar que ningun punto aprobado se ha quedado sin
construir**. Es el `CA-6` de la epica y existe porque `P-055` se perdio exactamente asi.

## Objetivo

Dar por cerrada la epica solo si lo que dice estar hecho lo esta, medido contra el sistema en marcha.

## Requisitos de usuario

### HU-1 — Cerrar sobre evidencia

**Como** Product Owner,
**quiero** que el cierre de la epica se sostenga en verificacion real,
**para** no repetir el patron de dar por resuelto lo que solo estaba anotado.

## Alcance (in-scope)

- Verificacion conducida de los seis criterios de aceptacion de la epica.
- Contraste numerico entre pantallas para `CA-2`: cifras de la misma campana en diario, cosechas,
  compras y Vision General, sacadas de la API con datos reales.
- Comprobacion de que los 25 puntos aprobados tienen historia que los recoge y evidencia de cierre.
- Correcciones de cierre acotadas en la propia rama de revision, segun el criterio ya establecido en el
  proyecto.
- Alta en `MVP-999` de los hallazgos que necesiten decision de producto o consumidor posterior.
- Actualizacion de las reglas de negocio modificadas, si alguna historia la dejo a medias.

## Fuera de alcance (out-of-scope)

- Implementar funcionalidad nueva.
- Reabrir decisiones ya tomadas en la clasificacion del 2026-08-06/07.

## Criterios de aceptación

- [x] **CA-1**: Tabla de veredicto con evidencia por cada criterio de aceptacion de `MVP-007`. Los
  seis, en el [tech-design](./tech-design.md), medidos contra el sistema en marcha y no releidos.
- [x] **CA-2**: Hallazgos numerados con evidencia reproducible. Nueve, de `R-01` a `R-09`, dados de
  alta como `P-096` … `P-104`. **Tres resultaron mayores de lo que decia el reporte que los origino**,
  precisamente por comprobarlos en vez de darlos por buenos.
- [x] **CA-3**: Trazabilidad punto a punto. Y aqui esta el hallazgo principal: **quince filas seguian
  en `aprobado-crear-historia` con su historia de destino ya `completado` y mergeada**, `P-055` entre
  ellas. Se verificaron una a una —las quince estaban construidas, no habia funcionalidad perdida— y se
  cerraron nombrando la evidencia de cada una.
- [x] **CA-4**: Los derivados quedan dados de alta como `P-xxx` con destino explicito. Los nueve, con
  destino `por decidir` porque son decision de producto: ninguno se asigna a una historia por
  iniciativa de la revision, que es como se empezo a perder `P-055`.
- [x] **CA-5**: `RN-006`, `RN-008`, `RN-009`, `RN-029` y `RN-041` reflejan el estado real. Comprobadas
  una a una contra el producto, no solo contra su redaccion: el boton «Actualizar» no existe
  (`RN-006`), el defecto de temporada lo resuelve el servidor y lo devuelve en `meta.scope` (`RN-008`),
  el quinto widget esta cableado en `VisionGeneralView` y su endpoint responde (`RN-009`), la cosecha
  admite `unit_price` con importe derivado y no almacenado (`RN-029`), y el plazo de 30 dias para los
  tokens muertos esta escrito con su razon (`RN-041`). Se anade `RN-043`, que `MVP-708` creo.

## Maquetas y referencias visuales

No aplica.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| Todas | Criterios de aceptacion de MVP-007 | hecho | Los seis verificados contra el sistema en marcha; el contraste numerico del `CA-2` se hizo sobre la API con datos que ejercitan ingresos e imputacion, no solo kg |

## Notas y decisiones

- Los hallazgos con filo salen de **contar y comparar cifras del sistema en marcha**, no de releer. En
  esta epica el contraste obvio es el de `CA-2`, que es el mismo metodo con el que se detecto `P-082`.
- **El contraste hubo que provocarlo.** Con los datos que habia, ninguna cosecha tenia precio y no
  existia ninguna imputacion, asi que el contraste habria pasado sin tocar nada de lo que `MVP-707` y
  `MVP-708` construyeron. Se creo el caso que **si** puede divergir —precio en dos partidas y un
  consumo imputado a una compra— y despues se restauro el estado anterior. Cuadro todo, incluido lo que
  mas importaba: el gasto **no duplica** el coste de una compra ya contada cuando parte de ella esta
  imputada.
- **El hallazgo principal no es de producto, es de proceso.** `P-096`: el registro de puntos no lo
  comprueba nadie, y por eso quince filas llevaban semanas diciendo que estaba pendiente algo que ya
  estaba hecho. El `CA-6` de la epica existe por `P-055`, y `P-055` volvia a estar en esa lista —esta
  vez construido, pero igual de mal anotado—. Mientras el registro dependa de que alguien se acuerde,
  se seguira perdiendo lo mismo.
- **Tres hallazgos crecieron al verificarlos**: los `.md` sin BOM pasaron de «probablemente haya mas» a
  74 de 210 con la regla sin respaldo del gate; los ficheros versionados de `artifacts/correos` pasaron
  de uno a once, contra lo que dice el propio `.gitignore`; y `formatDate` pasa de tres vistas a siete
  ficheros. Es el argumento para no cerrar una revision sobre informes ajenos.
