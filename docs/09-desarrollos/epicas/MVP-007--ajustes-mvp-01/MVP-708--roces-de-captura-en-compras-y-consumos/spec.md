---
id: "MVP-708"
tipo: feature
titulo: "Roces de captura en compras y consumos"
estado: borrador
prioridad: baja
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["operativa", "ux"]
  modulo_path: "03-modulos/"
  componentes: ["purchases", "consumptions"]
  etiquetas: ["mvp", "ajustes", "ux"]
  nivel_riesgo: bajo
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-708 — Roces de captura en compras y consumos

> **Origen**: `P-057` y `P-058` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

**`P-057`** — El alta de compra sugiere el vocabulario del historico (`GET /api/v1/purchases/products`,
`RN-031`), pero el formulario de consumo sin compra previa —el mismo campo de texto libre, en la misma
pantalla— no sugiere nada, porque las sugerencias solo miran `purchases`. Favorece justo la dispersion
de nombres que las sugerencias existen para evitar: «Abono NPK» comprado y «abono npk» consumido
conviven sin que nadie lo note.

**`P-058`** — Se admite imputar una compra con fecha **anterior** a la de la propia compra, sin aviso.
Verificado: imputando el 2020-01-01 una compra del 2026-07-31, responde `201`. No debe **bloquearse**
—la captura retroactiva es real y `RN-032` ya asume que el papeleo va por detras del campo— pero un
consumo anterior a su compra es casi siempre un error de tecleo en la fecha.

## Objetivo

Reducir dos roces del formulario que ensucian los datos sin que nadie se de cuenta.

## Requisitos de usuario

### HU-1 — Escribir el mismo nombre siempre

**Como** persona registrando consumos,
**quiero** que el campo me sugiera los materiales que ya uso,
**para** no acabar con tres nombres distintos del mismo producto.

### HU-2 — Enterarme de una fecha imposible

**Como** persona imputando una compra,
**quiero** que se me avise si la fecha del consumo es anterior a la de la compra,
**para** corregir el tecleo sin que me lo impidan cuando de verdad es asi.

## Alcance (in-scope)

- Las sugerencias de material combinan el historico de **compras y de consumos**, o el endpoint acepta
  un ambito.
- Aviso **no bloqueante** cuando la fecha del consumo es anterior a la de su compra: senal en el
  formulario y etiqueta en la fila, con la misma filosofia que `RN-023` usa para la temporada.
- Regla de negocio nueva documentada para ese aviso.

## Fuera de alcance (out-of-scope)

- Normalizacion avanzada de nombres (acentos, similitud), que sigue fuera de alcance desde `MVP-205`.
- Bloquear ninguna fecha: la captura retroactiva es legitima.

## Criterios de aceptación

- [ ] **CA-1**: El campo de material del consumo sin compra previa sugiere terminos del historico de
  compras **y** de consumos.
- [ ] **CA-2**: Imputar un consumo con fecha anterior a la de su compra sigue permitido y responde
  `201`, con aviso visible en el formulario.
- [ ] **CA-3**: La fila resultante lleva una etiqueta que lo senala, como ya hace «FUERA DE TEMPORADA».
- [ ] **CA-4**: La regla queda escrita en `docs/01-producto/reglas-de-negocio.md`.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/ComprasView.tsx](../../../../../prototype/terrenario-mvp/src/components/ComprasView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| ComprasView | RN-031, RN-032, RN-023 (analogia) | parcial | Sugerencias solo en compra; sin aviso de fecha |

## Notas y decisiones

- Los dos son del mismo formulario: separarlos obligaria a tocarlo dos veces.
