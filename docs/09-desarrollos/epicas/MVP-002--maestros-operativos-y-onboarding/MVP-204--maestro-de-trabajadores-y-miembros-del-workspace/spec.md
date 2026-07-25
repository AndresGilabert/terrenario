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
actualizado_en: "2026-07-25"
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

### HU-3 — Ver y retirar el acceso de los miembros del Workspace

**Como** miembro del Workspace,
**quiero** ver quién tiene acceso al Workspace y poder retirar el acceso a un miembro,
**para** mantener controlado quién opera sobre la explotación cuando alguien deja de colaborar.

## Alcance (in-scope)

- Alta, edición, listado e inactivación de trabajadores del Workspace.
- Exposición automática de miembros del Workspace como trabajadores seleccionables.
- Soporte de trabajadores sin cuenta vinculada.
- Base para usar tarifa horaria solo como referencia posterior, no como automatismo.
- Listado de miembros del Workspace con su estado de membresía (`activo`, `revocado`).
- Revocación del acceso de un miembro: transición de su membresía a `revocado`, de modo que deja de resolver contexto y de aparecer en el selector de Workspace (MVP-104).
- Reingreso de un miembro revocado por la vía normal de una nueva invitación (MVP-103), reutilizando su fila de membresía.

## Fuera de alcance (out-of-scope)

- Nómina, contratos o datos laborales avanzados.
- Automatización de costes a partir de tarifa horaria.
- Permisos diferenciados entre miembro y trabajador externo.
- Reactivación directa de un miembro revocado sin pasar por una invitación nueva.
- Transferencia de la propiedad (`workspace_owner`) del Workspace entre miembros.

## Criterios de aceptación

- [ ] **CA-1**: Los miembros del Workspace aparecen automáticamente como responsables seleccionables en el maestro de trabajadores.
- [ ] **CA-2**: El usuario puede crear y mantener trabajadores sin cuenta vinculada dentro del mismo Workspace.
- [ ] **CA-3**: Los trabajadores con histórico pueden inactivarse sin invalidar los registros ya existentes.
- [ ] **CA-4**: El usuario puede ver la lista de miembros del Workspace con su estado de membresía.
- [ ] **CA-5**: Al revocar el acceso de un miembro, su membresía pasa a `revocado` y deja de resolver contexto activo y de aparecer en su selector de Workspaces, sin borrar el vínculo ni invalidar los registros operativos que ese usuario ya hubiera creado.
- [ ] **CA-6**: El sistema impide dejar el Workspace sin ningún miembro activo: no se puede revocar al último miembro activo ni al `workspace_owner` mientras siga siendo el único propietario.

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
- La administración de miembros (HU-3, CA-4..CA-6) llega a esta historia desde el punto `P-002` del registro de `MVP-999`, detectado durante `MVP-104`. MVP-104 dejó ya modelado en el dominio el estado `revocado` del catálogo `worker_member_status` y el método `WorkspaceMember.Revoke()`, pero sin endpoint ni pantalla que lo usaran; esta historia expone ese comportamiento en API y UI.
- Coherente con permisos planos (RN-034): cualquier miembro activo puede revocar a otro. La única restricción es no dejar el Workspace sin miembro activo ni sin propietario (CA-6). La transferencia de propiedad queda fuera de alcance.
- Reingreso de un miembro revocado: se hace por la vía normal de una nueva invitación (MVP-103), que reutiliza la fila de membresía gracias al índice único `(workspace_id, user_id)`. No se ofrece reactivación directa para no abrir una segunda vía de alta paralela a las invitaciones.
