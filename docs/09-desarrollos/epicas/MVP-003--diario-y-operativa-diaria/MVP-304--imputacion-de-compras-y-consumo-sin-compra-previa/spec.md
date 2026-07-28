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
- **Modelo del consumo**: el consumo debe poder existir **sin compra**, con **fecha de negocio**
  propia y **temporada**, y con `producto` en texto libre cuando no hay compra de la que heredarlo.
  Es lo que hace realizables el CA-2 de esta historia y el CA-3 de la épica. Ver Notas.
- Concurrencia optimista y eliminación lógica del consumo, con el patrón que fija `MVP-301`.

## Fuera de alcance (out-of-scope)

- Repartos avanzados por fórmulas complejas.
- Reconciliación automática retroactiva.
- Control de stock y excedentes estructurado.

## Criterios de aceptación

- [ ] **CA-1**: Una compra puede imputarse a uno o varios terrenos con cantidad aproximada y coste proporcional.
- [ ] **CA-2**: El sistema permite registrar consumo sin compra previa, asignando coste 0 y mostrando aviso al usuario.
- [ ] **CA-3**: Registrar una compra posterior no recalcula automáticamente los consumos históricos ya guardados.
- [ ] **CA-4**: Un consumo registrado sin compra previa aparece en el diario en su **fecha de negocio** —no en la de captura— y queda asociado a una temporada, igual que una actividad o una compra (RN-021, RN-033).

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
- **Revisión previa (3ª pasada de `MVP-299`, 2026-07-28): el consumo sin compra previa no tenía dónde
  vivir** (hallazgo `G-2`). `RN-032` y el CA-3 de la épica lo exigen desde el principio, pero el ER
  declaraba `PURCHASE_CONSUMPTION.purchase_id` como FK **obligatoria** y la única ruta contratada
  colgaba de una compra (`POST /purchases/{id}/consumptions`). Corregido antes de arrancar:
  `purchase_id` pasa a anulable, se contrata `POST /api/v1/consumptions` y el ER recoge los campos que
  faltaban.
- **Decisión abierta, a cerrar en el `tech-design` de esta historia**: si el consumo sin compra se
  modela como `purchase_id` **anulable** sobre `PURCHASE_CONSUMPTION` o como **entidad de consumo
  propia** de la que la imputación es un caso particular. La revisión fija los **requisitos** —
  `purchase_id` opcional, coste `0` sin compra, sin recálculo retroactivo, fecha de negocio,
  temporada y producto libre— y deja el mecanismo a la implementación. Condiciona el modelo de
  `MVP-303`, así que debe decidirse **antes** de cerrarlo.
- **`G-3`: el consumo necesitaba fecha propia y temporada.** Solo tenía `created_at`, pero el diario
  de `MVP-305` ordena por fecha de negocio (RN-033) y `RN-021` exige temporada en toda la operativa.
  Un consumo capturado el lunes sobre trabajo del jueves anterior caía en el sitio equivocado.
