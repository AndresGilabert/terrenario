---
id: "MVP-399"
tipo: feature
titulo: "Revision epica"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito C — Registro operativo end-to-end"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-003--diario-y-operativa-diaria"
depende_de: ["MVP-301", "MVP-302", "MVP-303", "MVP-304", "MVP-305"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["gobernanza", "calidad", "scope-control"]
  modulo_path: "03-modulos/"
  componentes: ["backlog", "qa", "stabilization"]
  etiquetas: ["mvp", "revision-epica", "cierre"]
  nivel_riesgo: medio
creado_en: "2026-07-24"
actualizado_en: "2026-07-29"
---

# MVP-399 — Revision epica

## Contexto

Durante la ejecucion de una epica aparecen ajustes, puntos ciegos y necesidades no previstas en las historias originales. Si no se centralizan antes del cierre, se dispersan y se pierde trazabilidad para decidir el trabajo posterior.

## Objetivo

Ejecutar una revision final de la epica para validar el funcionamiento global, consolidar los pendientes detectados y convertirlos en nuevas historias planificables.

## Requisitos de usuario

### HU-1 — Consolidar pendientes de la epica

**Como** Product Owner,
**quiero** reunir en un solo punto los ajustes y requisitos detectados durante la epica,
**para** evitar omisiones y cerrar el alcance con trazabilidad.

### HU-2 — Verificar calidad funcional final

**Como** equipo de producto y desarrollo,
**quiero** revisar el estado final de la epica sobre el flujo integrado,
**para** abrir nuevas historias concretas con evidencias de error o falta.

## Alcance (in-scope)

- Revision integral del comportamiento entregado por la epica.
- Consolidacion de puntos ciegos y requisitos pendientes detectados durante las historias previas.
- Creacion de nuevas historias para cubrir errores, faltas o ajustes detectados.
- Priorizacion inicial de los nuevos items segun impacto funcional y de negocio.
- **Correcciones de cierre** detectadas en esta pasada, cuando no justifican una historia propia.
  Ampliacion acordada con el PO (2026-07-29), igual que en la 3a pasada de `MVP-299`; ver
  «Notas y decisiones».

## Fuera de alcance (out-of-scope)

- Implementar en esta historia los cambios que **si** justifican historia propia o una decision de
  producto pendiente (los hallazgos derivados a `MVP-999` y a `MVP-004`).
- Redefinir objetivos de negocio ya aprobados para la epica.
- Sustituir actividades de QA o validacion tecnica de historias previas.

## Criterios de aceptación

- [x] **CA-1**: Existe una revision funcional final de la epica con evidencias de lo validado.
- [x] **CA-2**: Todos los puntos ciegos o requisitos pendientes detectados quedan documentados.
- [x] **CA-3**: Cada punto pendiente se transforma en una nueva historia en la epica correspondiente o en MVP-999 cuando aplique.

## Revision funcional de la epica (2026-07-29)

Verificada sobre el flujo integrado real (API `:5127` + PostgreSQL + UI conducida `:5173`), con dos
Workspaces para comprobar el aislamiento.

| CA de la epica | Veredicto | Evidencia |
|---|---|---|
| **CA-1** Todas las historias en `completado` | cumplido | `MVP-301`..`MVP-305` cerradas con su `tech-design.md`; ver `_indice.md` |
| **CA-2** Registrar operativa diaria completa sin procesos externos ni calculos automaticos no cerrados | cumplido **con matiz** | Labor, compra, imputacion y consumo sin compra se registran sin salir de la aplicacion y sin ningun automatismo de coste (RN-003). El matiz es de literalidad: el CA dice «desde el diario» y la captura de compras y consumos vive en `/app/compras` por decision del PO. Ver hallazgo `R-07` |
| **CA-3** La ausencia de compra nunca bloquea el consumo, y el impacto en calidad del dato queda visible | cumplido | `POST /consumptions` no consulta ninguna compra (hay test que lo verifica); el aviso del formulario explica el coste 0 y que no habra recalculo; el diario y el libro muestran «N consumos sin compra previa» y cada fila lleva «sin compra» / «coste desconocido» |
| **CA-4** Dos personas no se pisan un registro operativo en silencio | cumplido | `version` + `If-Match` + `409 CONFLICT_VERSION_MISMATCH` con `current_version` en las tres entidades; conflicto **provocado desde la API con el formulario abierto** en las tres historias, con recarga y explicacion en el cliente |
| **CA-5** Ningun registro eliminado se pierde; confirmacion explicita y desaparicion de diario y listados | cumplido | Borrado logico verificado en base de datos (la fila permanece con `deleted_at`); dialogo que nombra el registro, con el foco inicial en «Cancelar»; el `422` de una compra con imputaciones vivas aparece dentro del dialogo |

Cobertura de reglas de negocio: RN-001, RN-002, RN-003, RN-021, RN-023, RN-025, RN-026, RN-031,
RN-032, RN-033 y RN-037 verificadas. `RN-033` queda **completa salvo la cosecha**, que no existe
hasta `MVP-004` (hallazgo `G-4` ya registrado): el catalogo `diary_entry_type` reserva el valor y
`MVP-401` la enciende.

Resultado tecnico: `dotnet test` en verde (478 tests), `npm run build` y `npm run lint` sin errores
nuevos, pipeline de KB del CI en verde.

## Hallazgos de la revision

| Hallazgo | Impacto | Descripcion | Destino |
|---|---|---|---|
| **R-01** | **alto** | **El resumen del diario contaba el mismo dinero dos veces.** `meta.total_cost` sumaba el coste de cada entrada, pero una imputacion reparte por terrenos dinero que **la compra ya aporto**: con datos reales, `788,19 €` = 281,99 (labores) + 448 (compras) + 58,20 (consumos), y esos 58,20 estaban dentro de los 448. Era la cifra de cabecera de la vista principal y el mismo criterio lo habria heredado el dashboard de `MVP-004`. **Decision del PO: el total excluye las imputaciones**; se publica `imputed_cost` aparte para que el reparto siga visible sin inflar el gasto | corregido aqui |
| **R-02** | bajo | **El diario no filtra por responsable** aunque el dato existe y `GET /activities` si lo soporta (`worker_id`). «Que hizo Antonio esta semana» no se puede responder desde la vista principal | `MVP-999` (`P-056`) |
| **R-03** | bajo | **El consumo sin compra previa no ofrece sugerencias de material**, mientras que el alta de compra si (`GET /purchases/products`). Es el mismo campo de texto libre en dos formularios contiguos, con ayuda en uno y sin ella en el otro, y favorece justo la dispersion de nombres que las sugerencias evitan | `MVP-999` (`P-057`) |
| **R-04** | bajo | **Se admite imputar una compra con fecha anterior a la de la propia compra**, sin aviso: verificado imputando el 2020-01-01 una compra del 2026-07-31 (`201`). No deberia bloquear —la captura retroactiva es real— pero encaja con la filosofia de RN-023: avisar sin impedir | `MVP-999` (`P-058`) |
| **R-05** | bajo | **Copy del Home desactualizado**: seguia diciendo que las compras «llegaran despues» cuando el modulo ya esta encendido | corregido aqui |
| **R-06** | medio | **El buscador de material del libro de compras no llegaba a los consumos**: filtraba la tabla de compras y dejaba la de consumos intacta, asi que buscar «gasoleo» mostraba una compra y todos los consumos de cualquier otra cosa | corregido aqui |
| **R-07** | doc | **El CA-2 de la epica dice «desde el diario»** y la decision del PO (2026-07-29) fue capturar compras y consumos en `/app/compras`, no en un modal unico del diario. No es un incumplimiento —la operativa se registra entera sin procesos externos, que es lo que la regla protege— pero conviene dejar escrita la lectura para que no se reabra | documentado |
| **R-08** | bajo | **`P-053`**, el foco que no volvia al campo tras anadir una tarea en `TareasView`, registrado en `MVP-303` con destino esta pasada | corregido aqui |

## Correcciones de cierre aplicadas

Cuatro cambios acotados, todos verificados end-to-end:

1. **`R-01`** — `DiaryQueryService` calcula el gasto **excluyendo las imputaciones** y publica
   `imputed_cost` aparte; el tile pasa a llamarse «Gasto» y anade «De ese gasto, X € ya estan
   repartidos por terrenos». Con los datos reales: `729,99 €` de gasto y `58,20 €` repartidos. Test
   de regresion que reproduce el hallazgo: una compra de 250 € repartida entera entre dos terrenos
   son 250 € de gasto, no 500.
2. **`R-06`** — `ConsumptionFilter` gana `Product` y el buscador del libro lo propaga. Verificado:
   «gas» devuelve 1 compra y 1 consumo, ambos de gasoleo.
3. **`R-05`** — copy del Home actualizado: el diario aparece como lo que es y solo quedan por llegar
   las cosechas y la vision general.
4. **`R-08` / `P-053`** — el foco vuelve al campo en `TareasView`, con el mismo patron que
   `ComprasView`: se pide por efecto tras el re-render, porque dentro del manejador el input sigue
   `disabled`. Verificado en UI conducida (`document.activeElement` vuelve a ser el campo).

## Maquetas y referencias visuales

- No aplica para esta historia de gobierno de alcance.

## Notas y decisiones

- Esta historia debe ejecutarse siempre como cierre de la epica.
- No se marca la epica como completada hasta cerrar esta historia.
- **Ampliacion de alcance acordada con el PO (2026-07-29)**: se admiten **correcciones de cierre** en
  esta misma historia cuando no justifican una historia propia, igual que en la 3a pasada de
  `MVP-299`. Se aplicaron cuatro (`R-01`, `R-05`, `R-06`, `R-08`); los tres hallazgos que si
  requieren decision de producto o un consumidor posterior (`R-02`, `R-03`, `R-04`) salen a
  `MVP-999` como `P-056`, `P-057` y `P-058`.
- **No se crean historias nuevas en `MVP-003`.** A diferencia de `MVP-002` —donde los hallazgos
  dieron lugar a `MVP-207` y `MVP-208`—, aqui ninguno rompe un CA de la epica ni bloquea a `MVP-004`:
  el unico de impacto alto (`R-01`) era una correccion de una linea de calculo, y el resto son
  mejoras con consumidor identificado mas adelante. Crear una historia para tres puntos menores
  habria sido ceremonia sin contenido.
- **Estado de los puntos de esta epica en `MVP-999`**: `P-028` y `P-050` **resueltos** (en `MVP-301`
  y `MVP-303`); `P-051` sigue abierto y **ampliado** —la paginacion obliga ademas a mover a SQL la
  mezcla en memoria del diario—; `P-052` **parcialmente resuelto** (terreno, temporada y tipo ya
  viajan al servidor; queda la busqueda por texto); `P-053` **resuelto aqui**; `P-054` y `P-055`
  siguen pendientes, con destino `MVP-999` y `MVP-502`.
- **Lo que hereda `MVP-004`**: el criterio de coste de `R-01` (una imputacion no es gasto nuevo) debe
  aplicarse igual en el dashboard; el diario ya reserva el tipo `cosecha` para `MVP-401`; y
  `P-051`/`P-052` se resuelven en `MVP-406` junto al resto de la navegacion.
