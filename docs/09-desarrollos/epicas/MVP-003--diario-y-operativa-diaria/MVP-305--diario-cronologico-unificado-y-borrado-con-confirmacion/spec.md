---
id: "MVP-305"
tipo: feature
titulo: "Diario cronológico unificado y borrado con confirmación"
estado: completado
prioridad: critica
sprint: ""
hito: "Hito C — Registro operativo end-to-end"
esfuerzo_estimado: "4d"
tickets: []
epica: "MVP-003--diario-y-operativa-diaria"
depende_de: ["MVP-301", "MVP-303", "MVP-304"]
bloquea: ["MVP-004", "MVP-006"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["diario", "ux-operativa"]
  modulo_path: "03-modulos/"
  componentes: ["diario", "actividades", "compras", "imputaciones"]
  etiquetas: ["mvp", "diario", "ux"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-29"
---

# MVP-305 — Diario cronológico unificado y borrado con confirmación

## Contexto

La KB deja claro que la vista principal del MVP debe ser un diario cronológico unificado y no un conjunto de módulos desconectados. Esa vista debe mostrar operativa real y soportar acciones básicas sin comprometer la seguridad funcional, incluyendo confirmación explícita antes del borrado, que es **lógico** (RN-037, corregida en la 3ª pasada de `MVP-299`).

## Objetivo

Ofrecer una vista principal única donde el usuario pueda consultar la operativa diaria del Workspace y gestionar acciones básicas de forma segura y comprensible.

## Requisitos de usuario

### HU-1 — Ver la operativa del Workspace en una sola vista

**Como** usuario operativo,
**quiero** consultar actividades, compras y consumos en orden cronológico,
**para** entender rápidamente qué ha pasado sin cambiar de pantalla.

### HU-2 — Eliminar un registro con seguridad

**Como** usuario del Workspace,
**quiero** que el borrado de un registro me pida confirmación,
**para** evitar errores accidentales sobre operativa ya capturada.

## Alcance (in-scope)

- Diario cronológico unificado del Workspace.
- Visualización conjunta de actividades, compras e imputaciones/consumos relevantes.
- Ordenación por **fecha de negocio** —`date` de la actividad, `purchase_date` de la compra y `date`
  del consumo—, no por fecha de captura, y lectura pensada para revisión operativa rápida.
- Confirmación explícita antes de eliminar un registro operativo, y desaparición inmediata del
  registro eliminado del diario y de los listados.

## Fuera de alcance (out-of-scope)

- Dashboard analítico o KPIs de producción.
- Edición masiva de registros desde el diario.
- Papelera, restauración o auditoría avanzada de borrados.

## Criterios de aceptación

- [x] **CA-1**: El usuario puede consultar en una sola vista cronológica la operativa relevante del Workspace.
- [x] **CA-2**: La vista del diario se alimenta de actividades, compras y consumos ya registrados en el MVP.
- [x] **CA-3**: Antes de eliminar un registro operativo, el sistema exige confirmación explícita del usuario, y el registro eliminado desaparece del diario y de los listados sin perderse en base de datos (eliminación lógica, RN-037).

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| DiarioView | RN-033 | cubierto | `GET /api/v1/diary` mezcla actividades, compras y consumos por fecha de negocio; verificado con 16 registros de los tres tipos en UI conducida |
| DiarioView | RN-037 | cubierto | Borrado logico con dialogo de confirmacion que nombra el registro y avisa de que no hay papelera; foco inicial en «Cancelar» |
| DiarioView | MVP-304 | cubierto | El 422 de una compra con imputaciones vivas aparece **dentro** del dialogo, que no se cierra |
| ComprasView | RN-037 | cubierto | El borrado con confirmacion tambien esta en los listados, no solo en el diario |

## Notas y decisiones

- Esta historia no sustituye al dashboard; define la experiencia principal de captura y revisión cotidiana.
- **Revisión previa (3ª pasada de `MVP-299`, 2026-07-28): el borrado es lógico, no físico**
  (hallazgo `G-1`). El texto original de esta historia y de la épica hablaba de «borrado físico»
  siguiendo a `RN-037`, pero el modelo de datos ya declaraba `deleted_at` en las tres entidades
  operativas y fijaba el borrado lógico como convención. **Decisión del PO: gana el modelo**; `RN-037`
  queda reformulada como «eliminación con confirmación explícita», la eliminación es lógica y el
  contrato publica el `DELETE` con esa semántica. Para el usuario el comportamiento visible es el
  mismo —confirma y el registro desaparece—; lo que cambia es que un borrado accidental no destruye el
  dato. No hay papelera ni restauración en el MVP: la purga se decide con la política de retención
  (`P-033`, `MVP-505`).
- **Decisiones de implementación (2026-07-29).** El diario es un **endpoint propio**
  (`GET /api/v1/diary`) y no una mezcla en el cliente: con tres listados, mezclar en el navegador
  significaría tres peticiones, orden frágil y la misma lógica repetida en cada consumidor futuro. La
  mezcla se hace **en memoria dentro del servidor** y no con un `UNION`, porque reutiliza los tres
  puertos ya probados y el diario todavía no pagina; resolver `P-051` obligará a moverla a SQL, y
  queda anotado allí. Se añade además `GET /api/v1/activities/{id}`, que el diario necesita para abrir
  el formulario de corrección sin cargarse el listado entero.
- **El borrado no vive en el diario**: cada tipo se elimina por su propio recurso, con su `If-Match`,
  para que las reglas propias —como la de `MVP-304` sobre compras con imputaciones vivas— sigan
  aplicando sin duplicarlas. El diálogo de confirmación es compartido y muestra el rechazo del
  servidor **sin cerrarse**, que es donde se está tomando la decisión.
- **El diario no incluye cosechas todavía** (hallazgo `G-4`). `RN-033` define la vista como la mezcla
  de actividades, **cosechas** y compras/consumos, pero `HARVEST` no existe hasta `MVP-004`. Esta
  historia entrega el diario con lo que hay y **`MVP-401` lo completa** encendiendo la cosecha; el
  diseño de la vista debe dejar sitio para ese tercer tipo de entrada en vez de asumir dos.
