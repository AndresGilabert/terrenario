---
id: "MVP-301"
tipo: feature
titulo: "Registro y edición de actividades"
estado: completado
prioridad: critica
sprint: ""
hito: "Hito C — Registro operativo end-to-end"
esfuerzo_estimado: "4d"
tickets: []
epica: "MVP-003--diario-y-operativa-diaria"
depende_de: ["MVP-202", "MVP-203", "MVP-204", "MVP-205"]
bloquea: ["MVP-302", "MVP-305"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["actividades", "diario"]
  modulo_path: "03-modulos/"
  componentes: ["actividades"]
  etiquetas: ["mvp", "operativa", "actividades"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-29"
---

# MVP-301 — Registro y edición de actividades

## Contexto

La actividad diaria es la unidad de captura más frecuente del MVP. La KB cierra que toda actividad debe incluir terreno, temporada, responsable, tarea, horas y coste manual, con tarea obligatoria y sin automatismos de coste.

## Objetivo

Permitir registrar y editar actividades completas del Workspace con la mínima fricción posible y con validaciones suficientes para garantizar trazabilidad útil.

## Requisitos de usuario

### HU-1 — Registrar una actividad completa

**Como** usuario operativo,
**quiero** registrar una actividad con responsable, tarea, tiempo y coste,
**para** dejar trazado qué se ha hecho en cada terreno.

### HU-2 — Corregir una actividad existente

**Como** usuario del Workspace,
**quiero** editar una actividad ya creada,
**para** corregir errores de captura sin repetir el registro completo.

## Alcance (in-scope)

- Alta de actividades con `fecha`, `terreno`, `temporada`, `trabajador`, `tarea`, `horas` y `coste_manual`, más `descripcion` opcional.
- Edición de actividades existentes.
- Autoselección de temporada activa en el formulario.
- Aviso si la fecha queda fuera del rango de la temporada elegida.
- Coste siempre manual/editable.
- Validaciones de obligatoriedad y coherencia de Workspace.
- **Concurrencia optimista** (`ADR-0005`): `ACTIVITY` estrena el patrón de las entidades operativas
  —`version` en el registro, `If-Match` obligatorio en `PATCH`/`DELETE` y `409
  CONFLICT_VERSION_MISMATCH`—, con el manejo del conflicto en el cliente.
- **Eliminación lógica** de la actividad (`deleted_at`, RN-037). La confirmación explícita en la UI y
  el borrado desde el diario son alcance de `MVP-305`; aquí se entrega la ruta y la semántica.

## Fuera de alcance (out-of-scope)

- Automatización de coste a partir de tarifa horaria.
- Sugerencias inteligentes de tareas por época.
- Captura de actividad offline.

## Criterios de aceptación

- [x] **CA-1**: Un usuario puede registrar una actividad con todos los campos obligatorios definidos por la KB.
- [x] **CA-2**: Si la fecha queda fuera del rango de la temporada seleccionada, el sistema muestra aviso pero no bloquea el guardado.
- [x] **CA-3**: El coste de actividad permanece siempre editable y no depende de cálculos automáticos obligatorios.
- [x] **CA-4**: Editar o eliminar una actividad con una versión desfasada responde `409 CONFLICT_VERSION_MISMATCH` en vez de sobrescribir en silencio, y el cliente resuelve el conflicto refrescando el registro (`ADR-0005`).
- [x] **CA-5**: El responsable de la actividad se elige del listado único `GET /api/v1/workers` y se guarda como `worker_id`, sea miembro del Workspace o cuadrilla sin cuenta, sin campos alternativos (cierre de `P-034` desde el lado del consumidor).

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| ActivityModal | RN-002, RN-003, RN-025 | cubierto | `ActivityFormModal`: responsable y horas obligatorios, coste manual editable con sugerencia de un clic y tarea del catalogo o texto libre. Verificado en UI conducida |
| ActivityModal | RN-021, RN-023 | cubierto | Temporada activa autoseleccionada y aviso no bloqueante de fecha fuera de rango, verificado en UI y por API (`is_out_of_season_range`) |
| DiarioView | RN-033 | cubierto | `/app/diario`: muro cronologico por fecha de negocio descendente, con filtros de terreno y temporada. La mezcla con compras y consumos es MVP-305 |
| DiarioView | ADR-0005 | cubierto | Conflicto de version provocado desde la API con el formulario abierto: el diario recarga y explica el cambio |

## Notas y decisiones

- Esta historia es la base operativa de la épica.
- **La identidad del responsable llega resuelta desde MVP-002.** El punto `P-034` de `MVP-999`
  («no hay identidad única de responsable») estaba registrado con destino a esta historia. Por
  decisión del PO (2026-07-28, 2ª pasada de `MVP-299`) se resuelve antes, en `MVP-208`: cada miembro
  del Workspace pasa a tener su fila en `workers`, de modo que aquí el responsable es siempre un
  `workers.id` y `ACTIVITY.worker_id` no necesita ser polimórfico. Esta historia **reutiliza** esa
  identidad y el listado único de `GET /api/v1/workers`; no debe reconstruirla ni combinar en cliente
  dos orígenes de personas.
- **Pendiente propio de esta historia**: `P-028`, cómo quedan `task_id?` y `task_text?` en `ACTIVITY`
  (FK opcional al catálogo de `MVP-205` más texto libre, RN-025) y la actualización del ER.
  **Resuelto aquí**: se materializan los **dos** campos como **excluyentes** —FK opcional a `tasks`
  con `ON DELETE RESTRICT` más texto libre acotado a la misma longitud que el nombre del catálogo,
  para que una tarea escrita al vuelo siempre quepa al guardarse en él (`MVP-302`)—, el dominio exige
  exactamente uno y la respuesta añade `task` ya resuelto. ER y contrato actualizados.
- **Decisiones de producto tomadas al arrancar la historia (PO, 2026-07-29)**: el diario se enciende
  como **sección propia** (`/app/diario`) y no sustituye al Home, para no adelantar la decisión que
  `P-040` asignó a `MVP-004`; y la captura **no** usa el modal único con pestañas del prototipo —el
  diario abre un formulario de actividad, y compras y consumos se capturan en su propia superficie
  (`MVP-303`/`MVP-304`)—. El detalle está en el `tech-design.md`.
- **Añadido en la revisión previa (3ª pasada de `MVP-299`, 2026-07-28).** `ACTIVITY` es la primera
  entidad crítica del MVP, así que estrena aquí dos decisiones que estaban en el aire:
  - **Concurrencia optimista** (`ADR-0005`, hallazgo `G-5`): el ADR estaba aceptado, el ER ya
    declaraba `version` y el contrato ya exigía `If-Match` con `409`, pero **ninguna historia del
    roadmap lo implementaba**. Decisión del PO: se hace aquí, no en `MVP-005`, para no reescribir
    después un cliente que no maneja el conflicto. El patrón que se fije aquí lo reutilizan `MVP-303`,
    `MVP-304` y `MVP-401`.
  - **Eliminación lógica** (`RN-037`, hallazgo `G-1`): la regla decía «borrado físico» y contradecía
    al modelo de datos, que ya declaraba `deleted_at`. Corregida a favor del borrado lógico.
  - `description` estaba contratado en `contratos-api.md` §5 y no figuraba en el ER ni en este alcance
    (hallazgo `G-6`): añadido en ambos.
