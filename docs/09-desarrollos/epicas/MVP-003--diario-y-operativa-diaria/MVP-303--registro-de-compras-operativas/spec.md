---
id: "MVP-303"
tipo: feature
titulo: "Registro de compras operativas"
estado: borrador
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
actualizado_en: "2026-07-20"
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
- Asociación de compra a temporada.
- Validaciones de cantidad y coste positivos.

## Fuera de alcance (out-of-scope)

- Catálogo formal de materiales.
- Stock disponible o saldo restante.
- Integración contable o fiscal.

## Criterios de aceptación

- [ ] **CA-1**: Un usuario puede registrar una compra con producto/material libre, cantidad, coste y temporada.
- [ ] **CA-2**: El sistema puede sugerir materiales previos del Workspace sin convertirlos en catálogo cerrado.
- [ ] **CA-3**: Las compras quedan disponibles para su imputación posterior a terrenos.

## Maquetas y referencias visuales

- Referencia funcional: RU-10 y RN-031.

## Notas y decisiones

- Esta historia prepara el dato de compra; no resuelve aún el consumo o la imputación.