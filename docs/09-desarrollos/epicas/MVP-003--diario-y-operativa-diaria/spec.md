---
id: "MVP-003"
tipo: epica
titulo: "Diario y operativa diaria"
estado: borrador
prioridad: critica
hito: "Hito C — Registro operativo end-to-end"
tickets: []
historias: ["MVP-301", "MVP-302", "MVP-303", "MVP-304", "MVP-305"]
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
actualizado_en: "2026-07-20"
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
- Confirmación explícita antes de borrado físico de registros operativos.

## Fuera de alcance

- Stock real, inventario vivo o saldos acumulados.
- Recalcular históricos cuando aparecen compras posteriores.
- Automatismos de coste desde tarifa horaria.
- Recomendaciones inteligentes de tareas por época o recurrencia.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [ ] **CA-2**: Un usuario puede registrar operativa diaria completa desde el diario sin depender de procesos externos ni de cálculos automáticos no cerrados.
- [ ] **CA-3**: La ausencia de compra previa nunca bloquea el registro de consumo, pero el sistema deja visible el impacto en calidad del dato.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-301` — Registro y edición de actividades.
- `MVP-302` — Guardado de tarea libre en catálogo del Workspace.
- `MVP-303` — Registro de compras operativas.
- `MVP-304` — Imputación de compras y consumo sin compra previa.
- `MVP-305` — Diario cronológico unificado y borrado con confirmación.

## Notas y decisiones

- Esta épica es la pieza clave para validar el tiempo de registro objetivo del MVP.
- El diario debe optimizar lectura y captura, no solo servir como listado pasivo.
