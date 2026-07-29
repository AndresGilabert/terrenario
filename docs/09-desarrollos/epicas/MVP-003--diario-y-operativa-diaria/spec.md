---
id: "MVP-003"
tipo: epica
titulo: "Diario y operativa diaria"
estado: completado
prioridad: critica
hito: "Hito C — Registro operativo end-to-end"
tickets: []
historias: ["MVP-301", "MVP-302", "MVP-303", "MVP-304", "MVP-305", "MVP-399"]
depende_de: ["MVP-001", "MVP-002"]
bloquea: ["MVP-004", "MVP-005", "MVP-006"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["actividades", "compras-consumo", "diario"]
  modulo_path: "03-modulos/"
  componentes: ["actividades", "compras", "imputaciones", "diario"]
  etiquetas: ["mvp", "operativa", "diario"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-29"
---

# EPICA MVP-003 — Diario y operativa diaria

## Contexto

La promesa principal del producto no es solo analítica, sino facilitar el registro cotidiano con una experiencia tipo diario. La KB cerró que el diario cronológico unificado es la vista principal del MVP, que la tarea es obligatoria y que el coste sigue siendo siempre manual/editable.

Esta épica entrega el primer valor operativo real y convierte la aplicación en sustituto funcional de la hoja en la parte más frecuente de uso.

## Objetivo

Permitir registrar y consultar el día a día del Workspace en una sola experiencia cronológica, incluyendo actividades y compras/consumos, sin bloquear la captura por ausencia de compra previa ni por configuración excesiva.

## Requisitos de usuario de alto nivel

- **Como** usuario operativo, **quiero** registrar rápidamente qué se ha hecho, quién lo ha hecho, cuánto ha costado y dónde ha ocurrido, **para** mantener trazabilidad diaria útil.
- **Como** usuario que revisa el trabajo reciente, **quiero** ver la actividad del Workspace en una vista cronológica unificada, **para** entender la operativa sin navegar por varios módulos aislados.

## Alcance

- Registro y edición de actividades con terreno, temporada, responsable, tarea, horas y coste manual.
- Validación de tarea obligatoria mediante catálogo o texto libre.
- Opción de guardar en catálogo una tarea introducida en texto libre.
- Registro de compras con producto/material libre y sugerencias desde histórico.
- Imputación de compras a terrenos con cantidad aproximada y coste proporcional.
- Permitir consumo sin compra previa con coste 0 y aviso.
- Diario cronológico unificado con actividades, compras y consumos.
- Confirmación explícita antes de eliminar un registro operativo. La eliminación es **lógica**
  (`deleted_at`), no física: RN-037 corregida en la revisión previa (ver Notas).
- **Concurrencia optimista** en las entidades operativas que nacen aquí: `version` en el registro,
  `If-Match` en `PATCH`/`DELETE` y `409 CONFLICT_VERSION_MISMATCH` (`ADR-0005`). Ver Notas.

## Fuera de alcance

- Stock real, inventario vivo o saldos acumulados.
- Recalcular históricos cuando aparecen compras posteriores.
- Automatismos de coste desde tarifa horaria.
- Recomendaciones inteligentes de tareas por época o recurrencia.

## Criterios de aceptación de la épica

- [x] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [x] **CA-2**: Un usuario puede registrar operativa diaria completa desde el diario sin depender de procesos externos ni de cálculos automáticos no cerrados.
- [x] **CA-3**: La ausencia de compra previa nunca bloquea el registro de consumo, pero el sistema deja visible el impacto en calidad del dato.
- [x] **CA-4**: Dos personas del mismo Workspace no pueden pisarse un registro operativo en silencio: la edición y el borrado exigen la versión vigente y responden `409 CONFLICT_VERSION_MISMATCH` si no lo es (`ADR-0005`).
- [x] **CA-5**: Ningún registro operativo eliminado se pierde: la eliminación es lógica y exige confirmación explícita (RN-037), y lo eliminado deja de aparecer en el diario y en los listados.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-301` — Registro y edición de actividades.
- `MVP-302` — Guardado de tarea libre en catálogo del Workspace.
- `MVP-303` — Registro de compras operativas.
- `MVP-304` — Imputación de compras y consumo sin compra previa.
- `MVP-305` — Diario cronológico unificado y borrado con confirmación.
- `MVP-399` — Revision epica.

## Vinculacion con prototipo (fuente visual)

Regla de precedencia para todas las historias de esta epica:

- La fuente de verdad funcional y de requisitos es la KB.
- El prototipo solo aporta referencia visual, estructura de pantallas y flujos UX.
- Si hay contradiccion, prevalece la KB.

Referencia base del prototipo:

- [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)
- [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)
- [prototype/reports/mvp-prototype-coverage.md](../../../../prototype/reports/mvp-prototype-coverage.md)

Matriz historia -> pantallas/componentes:

| Historia | Referencias de prototipo | Cobertura |
|---|---|---|
| MVP-301 | [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx), [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx) | **Cubierto**: `/app/diario` con alta y correccion de actividad |
| MVP-302 | [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx) | **Cubierto**: casilla de guardado en el catalogo durante la captura y accion de promocion en la tarjeta del diario |
| MVP-303 | [prototype/terrenario-mvp/src/components/ComprasView.tsx](../../../../prototype/terrenario-mvp/src/components/ComprasView.tsx) | **Cubierto**: `/app/compras` con gasto acumulado, alta en linea y sugerencias de material |
| MVP-304 | [prototype/terrenario-mvp/src/components/ComprasView.tsx](../../../../prototype/terrenario-mvp/src/components/ComprasView.tsx), [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx) | **Cubierto**: imputacion por fila con coste proyectado y consumo sin compra con aviso de coste 0 |
| MVP-305 | [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx) | **Cubierto**: diario unificado por fecha de negocio y borrado logico con confirmacion explicita |

## Notas y decisiones

- Esta épica es la pieza clave para validar el tiempo de registro objetivo del MVP.
- El diario debe optimizar lectura y captura, no solo servir como listado pasivo.
- **Revisión previa al arranque (3ª pasada de `MVP-299`, 2026-07-28).** Antes de abrir la épica se
  revisaron sus cinco historias contra la KB y se corrigieron cuatro huecos y contradicciones que
  habrían aflorado durante la construcción:
  - **`G-1` · Borrado.** `RN-037` decía «borrado **físico**» y el modelo de datos declara `deleted_at`
    y fija el borrado lógico como convención de las entidades operativas. **Decisión del PO: gana el
    borrado lógico**; `RN-037` queda reformulada y el contrato publica el `DELETE` como baja lógica.
    Afecta a `MVP-301`, `MVP-303`, `MVP-304` y sobre todo a `MVP-305` (CA-3).
  - **`G-2` · Consumo sin compra previa.** Es el CA-3 de esta épica, pero `PURCHASE_CONSUMPTION`
    declaraba `purchase_id` obligatorio y la única ruta contratada colgaba de una compra: la excepción
    no tenía dónde vivir. `purchase_id` pasa a ser anulable y se contrata `POST /consumptions`. El
    mecanismo lo cierra el `tech-design` de `MVP-304`.
  - **`G-3` · El consumo no tenía fecha de negocio ni temporada**, solo `created_at`, y el diario lo
    ordena cronológicamente (RN-033) mientras `RN-021` exige temporada. Añadidos al ER.
  - **`G-5` · Concurrencia sin dueño.** `ADR-0005` está aceptado y el contrato exige `If-Match` con
    `409`, pero ninguna historia del roadmap lo implementaba. **Decisión del PO: se implementa en esta
    épica**, con las entidades que la estrenan, en vez de retrofitarlo en `MVP-005` sobre un cliente
    ya escrito sin manejo de conflicto. `MVP-401` hereda el mismo patrón para `HARVEST`.
- **`RN-033` se completa en `MVP-004`.** El diario que entrega `MVP-305` mezcla actividades, compras
  y consumos; las **cosechas** todavía no existen. Encenderlas en el diario es alcance de `MVP-401`,
  no una omisión de esta épica (hallazgo `G-4`). El catálogo `diary_entry_type` ya reserva el valor
  `cosecha`, así que `MVP-401` la enciende añadiendo un puerto y un icono.
- **Cierre de la épica (`MVP-399`, 2026-07-29).** Las cinco historias funcionales quedaron cerradas y
  la revisión final verificó los cinco CA sobre el flujo integrado real. Salieron ocho hallazgos:
  cuatro se corrigieron como cierre —el más relevante, `R-01`: el resumen del diario **contaba dos
  veces el mismo dinero**, porque sumaba el coste de una compra y además el de sus imputaciones—, tres
  se derivaron a `MVP-999` (`P-056`, `P-057`, `P-058`) y uno era documental. **No se abrieron
  historias nuevas en esta épica**: ninguno rompía un CA ni bloqueaba a `MVP-004`. El detalle está en
  el `spec.md` de `MVP-399`.
- **Lo que hereda `MVP-004`**: el criterio de coste de `R-01` (una imputación reparte dinero ya
  contado, no es gasto nuevo) debe aplicarse igual en el dashboard.
