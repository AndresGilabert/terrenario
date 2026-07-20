---
id: "MVP-402"
tipo: feature
titulo: "Reglas de producción, catálogo y destinos"
estado: borrador
prioridad: critica
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-401"]
bloquea: ["MVP-403", "MVP-404", "MVP-405"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["produccion", "validaciones"]
  modulo_path: "03-modulos/"
  componentes: ["cosechas", "catalogos", "validaciones"]
  etiquetas: ["mvp", "produccion", "reglas"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# MVP-402 — Reglas de producción, catálogo y destinos

## Contexto

No basta con poder guardar cosechas: la producción del MVP debe seguir reglas cerradas para ser comparable. La KB fija catálogo global de productos, destino cerrado con `desconocido`, unidad canónica de rendimiento y exclusión mutua entre `rendimiento` y `litros`.

## Objetivo

Garantizar que las cosechas se registran bajo reglas homogéneas y compatibles con el cálculo posterior de KPIs del dashboard.

## Requisitos de usuario

### HU-1 — Registrar producción con reglas coherentes

**Como** usuario del Workspace,
**quiero** que la app valide producto, destino y rendimiento de forma consistente,
**para** evitar datos ambiguos o incompatibles entre sí.

### HU-2 — No bloquear una cosecha por falta de destino final

**Como** usuario operativo,
**quiero** poder usar `desconocido` como destino,
**para** no retrasar el registro cuando todavía no conozco el cierre comercial o de uso.

## Alcance (in-scope)

- Catálogo global fijo de productos de cosecha.
- Validación de producto obligatorio.
- Taxonomía cerrada de destinos, incluyendo `desconocido`.
- Regla XOR entre `rendimiento` y `litros`.
- Soporte de unidad canónica L/100kg y entradas equivalentes de rendimiento.

## Fuera de alcance (out-of-scope)

- Gestión de catálogo editable por usuarios.
- Fórmulas económicas derivadas de precio o balance.
- Reglas avanzadas de molturación o integración con almazaras.

## Criterios de aceptación

- [ ] **CA-1**: El sistema exige producto válido, `kgs` y destino dentro del marco funcional del MVP.
- [ ] **CA-2**: `rendimiento` y `litros` no pueden coexistir en la misma cosecha.
- [ ] **CA-3**: `desconocido` se admite como destino válido sin degradar la consistencia posterior del dashboard.

## Maquetas y referencias visuales

- Referencia funcional: RN-012, RN-013, RN-014, RN-029 y RN-030.

## Notas y decisiones

- Esta historia cierra la semántica de producción del MVP antes de explotar datos en dashboard.