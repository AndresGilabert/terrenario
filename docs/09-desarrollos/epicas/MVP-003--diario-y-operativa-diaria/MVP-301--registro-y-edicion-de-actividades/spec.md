---
id: "MVP-301"
tipo: feature
titulo: "Registro y edición de actividades"
estado: borrador
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
actualizado_en: "2026-07-21"
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

- Alta de actividades con `fecha`, `terreno`, `temporada`, `trabajador`, `tarea`, `horas` y `coste_manual`.
- Edición de actividades existentes.
- Autoselección de temporada activa en el formulario.
- Aviso si la fecha queda fuera del rango de la temporada elegida.
- Coste siempre manual/editable.
- Validaciones de obligatoriedad y coherencia de Workspace.

## Fuera de alcance (out-of-scope)

- Automatización de coste a partir de tarifa horaria.
- Sugerencias inteligentes de tareas por época.
- Captura de actividad offline.

## Criterios de aceptación

- [ ] **CA-1**: Un usuario puede registrar una actividad con todos los campos obligatorios definidos por la KB.
- [ ] **CA-2**: Si la fecha queda fuera del rango de la temporada seleccionada, el sistema muestra aviso pero no bloquea el guardado.
- [ ] **CA-3**: El coste de actividad permanece siempre editable y no depende de cálculos automáticos obligatorios.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| ActivityModal | RN-002, RN-003, RN-025 | parcial | Formulario de actividad disponible |
| DiarioView | RN-033 | cubierto | Visualizacion cronologica del diario |

## Notas y decisiones

- Esta historia es la base operativa de la épica.
