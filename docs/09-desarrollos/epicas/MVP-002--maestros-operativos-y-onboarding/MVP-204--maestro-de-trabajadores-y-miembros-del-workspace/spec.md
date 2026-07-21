---
id: "MVP-204"
tipo: feature
titulo: "Maestro de trabajadores y miembros del Workspace"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-104"]
bloquea: ["MVP-003"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["trabajadores", "workspaces"]
  modulo_path: "03-modulos/"
  componentes: ["trabajadores", "workspace-members"]
  etiquetas: ["mvp", "masters", "trabajadores"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-204 — Maestro de trabajadores y miembros del Workspace

## Contexto

El registro de actividades exige un responsable y la KB cierra que todos los miembros del Workspace deben aparecer automáticamente como trabajadores seleccionables, manteniendo además la posibilidad de trabajadores sin cuenta vinculada.

## Objetivo

Dejar preparado un maestro de trabajadores reutilizable y coherente con la membresía del Workspace para no introducir inconsistencia en responsables operativos.

## Requisitos de usuario

### HU-1 — Reutilizar responsables consistentes

**Como** usuario que registra actividad,
**quiero** elegir responsables desde un maestro común,
**para** evitar nombres duplicados o inconsistentes.

### HU-2 — Combinar miembros internos y trabajadores externos

**Como** usuario del Workspace,
**quiero** contar tanto con miembros del Workspace como con trabajadores sin cuenta,
**para** reflejar la realidad operativa de la explotación.

## Alcance (in-scope)

- Alta, edición, listado e inactivación de trabajadores del Workspace.
- Exposición automática de miembros del Workspace como trabajadores seleccionables.
- Soporte de trabajadores sin cuenta vinculada.
- Base para usar tarifa horaria solo como referencia posterior, no como automatismo.

## Fuera de alcance (out-of-scope)

- Nómina, contratos o datos laborales avanzados.
- Automatización de costes a partir de tarifa horaria.
- Permisos diferenciados entre miembro y trabajador externo.

## Criterios de aceptación

- [ ] **CA-1**: Los miembros del Workspace aparecen automáticamente como responsables seleccionables en el maestro de trabajadores.
- [ ] **CA-2**: El usuario puede crear y mantener trabajadores sin cuenta vinculada dentro del mismo Workspace.
- [ ] **CA-3**: Los trabajadores con histórico pueden inactivarse sin invalidar los registros ya existentes.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/TrabajadoresView.tsx](../../../../../prototype/terrenario-mvp/src/components/TrabajadoresView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/ActivityModal.tsx](../../../../../prototype/terrenario-mvp/src/components/ActivityModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| TrabajadoresView | RN-027 | parcial | Maestro de trabajadores operativo en UI |
| ActivityModal | RN-002, RN-027 | parcial | Seleccion de responsable disponible |

## Notas y decisiones

- La relación exacta con coste por tarifa se resuelve después en operativa diaria; aquí solo se prepara el maestro.
