---
id: "MVP-303"
tipo: feature
titulo: "Registro de compras operativas"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito C — Registro operativo end-to-end"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-003--diario-y-operativa-diaria"
depende_de: ["MVP-203"]
bloquea: ["MVP-304", "MVP-305"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["compras-consumo"]
  modulo_path: "03-modulos/"
  componentes: ["compras"]
  etiquetas: ["mvp", "compras", "operativa"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-29"
---

# MVP-303 — Registro de compras operativas

## Contexto

El MVP debe registrar compras de materiales sin exigir catálogo cerrado ni stock complejo. La KB fija producto/material en texto libre con sugerencias desde histórico y asociación obligatoria a temporada.

## Objetivo

Permitir registrar compras operativas del Workspace con el mínimo de datos necesarios para luego poder imputarlas o consultarlas en contexto.

## Requisitos de usuario

### HU-1 — Registrar una compra de material

**Como** usuario operativo,
**quiero** registrar una compra con producto, cantidad, coste y temporada,
**para** mantener trazabilidad mínima de materiales y gasto.

### HU-2 — Reutilizar vocabulario de compras previas

**Como** usuario recurrente,
**quiero** recibir sugerencias de materiales ya usados,
**para** escribir menos y mantener consistencia básica sin catálogo rígido.

## Alcance (in-scope)

- Alta, edición y listado de compras del Workspace.
- Producto/material como texto libre.
- Sugerencias de valores desde histórico del Workspace.
- Asociación de compra a temporada (`season_id`, RN-021). Materializa `P-050`: el contrato ya lo
  exigía y el ER no lo declaraba.
- Validaciones de cantidad y coste positivos.
- Concurrencia optimista y eliminación lógica de la compra, con el patrón que fija `MVP-301`
  (`version` + `If-Match` + `409`; `deleted_at` por RN-037).

## Fuera de alcance (out-of-scope)

- Catálogo formal de materiales.
- Stock disponible o saldo restante.
- Integración contable o fiscal.

## Criterios de aceptación

- [x] **CA-1**: Un usuario puede registrar una compra con producto/material libre, cantidad, coste y temporada.
- [x] **CA-2**: El sistema puede sugerir materiales previos del Workspace sin convertirlos en catálogo cerrado.
- [x] **CA-3**: Las compras quedan disponibles para su imputación posterior a terrenos.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/ComprasView.tsx](../../../../../prototype/terrenario-mvp/src/components/ComprasView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| ComprasView | RN-031 | cubierto | `/app/compras`: producto en texto libre con sugerencias del historico en `datalist`, verificado en UI conducida |
| ComprasView | RN-021, RN-023 | cubierto | Temporada obligatoria (la activa por defecto) y aviso no bloqueante de fecha fuera de rango |
| ComprasView | RN-003 | cubierto | Coste siempre manual; el precio unitario se deriva de coste/cantidad y se muestra en vivo |
| ComprasView | ADR-0005 | cubierto | Conflicto de version provocado desde la API con el modal abierto: el libro recarga y lo explica |
| DiarioView | RN-033 | falta | Las compras entran en el diario unificado en MVP-305 |

## Notas y decisiones

- Esta historia prepara el dato de compra; no resuelve aún el consumo o la imputación.
- **Revisión previa (3ª pasada de `MVP-299`, 2026-07-28).** Dos ajustes antes de arrancar:
  - `PURCHASE` gana `season_id` en el ER (`P-050`, hallazgo `R-28`): `RN-021` lo exige y
    `contratos-api.md` §7 ya lo contrataba como `season_id*`, pero el ER solo referenciaba
    `workspace_id`. Es el equivalente para la compra de lo que `P-028` registra para la tarea.
  - El **modelo del consumo condiciona a esta historia**: `MVP-304` necesita que un consumo pueda
    existir sin compra (RN-032), así que la decisión de si `purchase_id` es una columna anulable o una
    entidad propia debe tomarse **antes** de cerrar el modelo de compras, no después. Ver `MVP-304`.
    **Decidido al implementar esta historia (2026-07-29): `purchase_id` anulable sobre
    `PURCHASE_CONSUMPTION`, no una entidad propia.** Una imputación y un consumo sin compra son el
    mismo hecho —lo único que cambia es de dónde sale el coste—, y separarlos obligaría al diario
    (`MVP-305`) y al dashboard (`MVP-004`) a unir dos tablas con las mismas columnas. Consecuencias ya
    preparadas aquí: `unit_price` se **persiste** en la compra (base del coste proporcional, y permite
    explicar una imputación antigua aunque la compra se edite después) y el consumo guardará su propio
    `product`. El razonamiento completo está en el `tech-design.md`.
