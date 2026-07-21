---
id: "MVP-305"
tipo: feature
titulo: "Diario cronológico unificado y borrado con confirmación"
estado: borrador
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
actualizado_en: "2026-07-21"
---

# MVP-305 — Diario cronológico unificado y borrado con confirmación

## Contexto

La KB deja claro que la vista principal del MVP debe ser un diario cronológico unificado y no un conjunto de módulos desconectados. Esa vista debe mostrar operativa real y soportar acciones básicas sin comprometer la seguridad funcional, incluyendo confirmación explícita antes del borrado físico.

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
- Ordenación por fecha y lectura pensada para revisión operativa rápida.
- Confirmación explícita antes del borrado físico de registros operativos.

## Fuera de alcance (out-of-scope)

- Dashboard analítico o KPIs de producción.
- Edición masiva de registros desde el diario.
- Papelera, restauración o auditoría avanzada de borrados.

## Criterios de aceptación

- [ ] **CA-1**: El usuario puede consultar en una sola vista cronológica la operativa relevante del Workspace.
- [ ] **CA-2**: La vista del diario se alimenta de actividades, compras y consumos ya registrados en el MVP.
- [ ] **CA-3**: Antes de borrar un registro operativo, el sistema exige confirmación explícita del usuario.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| DiarioView | RN-033 | cubierto | Timeline cronologico unificado implementado |
| DiarioView | RN-037 | falta | Borrado existe pero sin confirmacion explicita |

## Notas y decisiones

- Esta historia no sustituye al dashboard; define la experiencia principal de captura y revisión cotidiana.
