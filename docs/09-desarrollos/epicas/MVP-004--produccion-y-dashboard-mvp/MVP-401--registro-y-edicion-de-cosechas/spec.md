---
id: "MVP-401"
tipo: feature
titulo: "Registro y edición de cosechas"
estado: borrador
prioridad: critica
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "4d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-202", "MVP-203", "MVP-305"]
bloquea: ["MVP-402", "MVP-403", "MVP-404", "MVP-405"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["produccion", "cosechas"]
  modulo_path: "03-modulos/"
  componentes: ["cosechas"]
  etiquetas: ["mvp", "produccion", "cosecha"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-401 — Registro y edición de cosechas

## Contexto

La producción es la materia prima del dashboard MVP. La KB ya cerró que toda cosecha debe incluir `producto`, `kgs`, `destino`, `temporada` y solo uno entre `rendimiento` o `litros`, evitando mezclar todavía precio, balance o molturación.

## Objetivo

Permitir registrar y editar cosechas con un modelo simple, consistente y alineado con el alcance operativo del MVP.

## Requisitos de usuario

### HU-1 — Registrar una cosecha del Workspace

**Como** usuario operativo,
**quiero** registrar una cosecha con los datos mínimos definidos,
**para** transformar la producción real en información utilizable por la aplicación.

### HU-2 — Corregir una cosecha existente

**Como** usuario del Workspace,
**quiero** editar una cosecha ya guardada,
**para** corregir errores sin duplicar registros.

## Alcance (in-scope)

- Alta de cosechas con `fecha`, `terreno`, `temporada`, `producto`, `kgs`, `destino` y uno entre `rendimiento` o `litros`.
- Edición de cosechas existentes.
- Autoselección de temporada activa en el formulario.
- Aviso no bloqueante si la fecha cae fuera del rango de la temporada seleccionada.

## Fuera de alcance (out-of-scope)

- Precio, balance, molturación y datos económico-industriales.
- Captura offline de cosechas.
- Analítica avanzada de producción.

## Criterios de aceptación

- [ ] **CA-1**: Un usuario puede registrar una cosecha con todos los campos obligatorios definidos para MVP.
- [ ] **CA-2**: El sistema permite editar cosechas existentes manteniendo coherencia de Workspace, terreno y temporada.
- [ ] **CA-3**: Si la fecha queda fuera del rango de temporada, el sistema avisa pero no bloquea el guardado.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/CosechasView.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechasView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| CosechaModal | RN-029, RN-004 | parcial | Formulario de cosecha disponible |
| CosechasView | RN-029 | cubierto | Listado de cosechas y borrado visual |

## Notas y decisiones

- Esta historia entrega la base de datos sobre la que se apoyará el dashboard.
