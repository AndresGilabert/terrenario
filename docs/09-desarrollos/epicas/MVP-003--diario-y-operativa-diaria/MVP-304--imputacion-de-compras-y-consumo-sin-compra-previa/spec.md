---
id: "MVP-304"
tipo: feature
titulo: "Imputación de compras y consumo sin compra previa"
estado: borrador
prioridad: critica
sprint: ""
hito: "Hito C — Registro operativo end-to-end"
esfuerzo_estimado: "4d"
tickets: []
epica: "MVP-003--diario-y-operativa-diaria"
depende_de: ["MVP-202", "MVP-303"]
bloquea: ["MVP-305"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["compras-consumo", "trazabilidad"]
  modulo_path: "03-modulos/"
  componentes: ["imputaciones", "compras", "consumos"]
  etiquetas: ["mvp", "consumo", "coste"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-304 — Imputación de compras y consumo sin compra previa

## Contexto

La trazabilidad útil del MVP no termina en la compra: debe poder imputarse a terrenos con cantidad aproximada y coste proporcional. Además, la KB ya cierra que el consumo no puede bloquearse por no existir compra previa y que no se recalculan históricos cuando la compra aparece después.

## Objetivo

Permitir repartir compras por terrenos y registrar consumo operativo incluso cuando la compra aún no exista, manteniendo claridad sobre el impacto en calidad del dato.

## Requisitos de usuario

### HU-1 — Imputar una compra a terrenos

**Como** usuario del Workspace,
**quiero** repartir una compra entre terrenos,
**para** saber dónde se ha consumido el material y qué coste proporcional representa.

### HU-2 — Registrar consumo sin compra previa

**Como** usuario operativo,
**quiero** registrar un consumo aunque aún no haya dado de alta la compra,
**para** no perder la trazabilidad del trabajo real por una dependencia administrativa.

## Alcance (in-scope)

- Imputación de una compra a uno o varios terrenos con cantidad aproximada.
- Cálculo del coste proporcional asociado a cada imputación.
- Registro operativo con coste 0 y aviso cuando todavía no existe compra previa.
- No recalcular imputaciones históricas cuando una compra aparece más tarde.

## Fuera de alcance (out-of-scope)

- Repartos avanzados por fórmulas complejas.
- Reconciliación automática retroactiva.
- Control de stock y excedentes estructurado.

## Criterios de aceptación

- [ ] **CA-1**: Una compra puede imputarse a uno o varios terrenos con cantidad aproximada y coste proporcional.
- [ ] **CA-2**: El sistema permite registrar consumo sin compra previa, asignando coste 0 y mostrando aviso al usuario.
- [ ] **CA-3**: Registrar una compra posterior no recalcula automáticamente los consumos históricos ya guardados.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/ComprasView.tsx](../../../../../prototype/terrenario-mvp/src/components/ComprasView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| ComprasView | RN-032 | falta | No existe imputacion de compra por terreno |
| DiarioView | RN-032 | falta | No hay flujo de consumo sin compra previa con aviso |

## Notas y decisiones

- Esta historia contiene la excepción operativa más importante de la épica y debe quedar especialmente bien validada.
