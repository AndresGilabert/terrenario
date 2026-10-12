---
id: "PRD-100"
tipo: epica
titulo: "Dashboard listo para la promoción"
estado: en-progreso
prioridad: alta
hito: "Post-MVP — Preparación para la promoción"
tickets: []
historias: ['PRD-101']
depende_de: ["MVP-008"]
bloquea: []
relacionado_con: ["MKT-108"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "analitica", "marketing"]
  modulo_path: "03-modulos/produccion-y-dashboard"
  componentes: ["dashboard", "evolucion-rendimiento"]
  etiquetas: ["promocion", "dashboard", "bkl-002"]
  nivel_riesgo: medio
creado_en: "2026-10-12"
actualizado_en: "2026-10-12"
---

# EPICA PRD-100 — Dashboard listo para la promoción

## Contexto

El dashboard es la pantalla más llamativa del producto y la que se quiere usar para promocionarlo. El
widget «Evolución del rendimiento» no se comporta como el producto necesita: agrupa por mes y compara
con una media histórica fija, que no sirve para decidir cuándo cosechar. El PO lo registró como
`BKL-002` el 2026-10-10 y lo marcó como bloqueo de la promoción.

## Objetivo

Que el dashboard pueda mostrarse en la promoción sin un widget que contradiga lo que el producto
promete. La épica empieza con la corrección de ese widget y se amplía solo con lo que el refinamiento
confirme que bloquea la promoción.

## Alcance

- Sustitución de la evolución del rendimiento por una serie diaria con regresiones lineales (`PRD-101`).

## Fuera de alcance

- Widgets nuevos o analítica avanzada: el MVP los excluye (`vision-y-objetivos.md`).
- Modelo de producción ampliado (`P-059` a `P-063`): no es necesario para esta corrección.
- Pendientes todavía sin refinar y sin destino decidido: `BKL-003` y `BKL-004` (defectos móviles),
  captura promocional del dashboard (`MKT-108`), `BKL-001` y `BKL-005`. Se incorporan aquí o se
  planifican aparte cuando el PO lo decida.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [ ] **CA-2**: El widget de evolución del rendimiento cumple los criterios de `PRD-101`, verificado sobre
  el sistema en marcha con datos sintéticos.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.
