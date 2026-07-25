---
id: "MVP-204"
tipo: feature
titulo: "Maestro de trabajadores y miembros del Workspace"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "5d"
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

### HU-3 — Ver quién pertenece al Workspace y en qué estado

**Como** miembro del Workspace,
**quiero** ver la lista de personas del Workspace con su estado de membresía (`invitado`, `activo`, `revocado`),
**para** saber en todo momento quién tiene acceso, quién está pendiente de aceptar y quién dejó de colaborar.

### HU-4 — Retirar el acceso de un miembro

**Como** miembro del Workspace,
**quiero** poder retirar el acceso a un miembro activo,
**para** mantener controlado quién opera sobre la explotación cuando alguien deja de colaborar.

### HU-5 — Reenviar la invitación a una persona pendiente

**Como** miembro del Workspace,
**quiero** reenviar la invitación de una persona en estado `invitado`, por email o por enlace,
**para** insistir cuando no ha llegado o ha caducado, exactamente igual que la primera vez.

## Alcance (in-scope)

- Alta, edición, listado e inactivación de trabajadores del Workspace.
- Exposición automática de miembros del Workspace como trabajadores seleccionables.
- Soporte de trabajadores sin cuenta vinculada.
- Base para usar tarifa horaria solo como referencia posterior, no como automatismo.
- Listado unificado de personas del Workspace con su estado de membresía (`invitado`, `activo`, `revocado`), donde `invitado` corresponde a las invitaciones por email todavía pendientes (MVP-103).
- Materialización del estado `invitado` en el flujo: una invitación por email pendiente se ve como una persona del Workspace en estado `invitado`, y al aceptarse pasa a `activo`.
- Revocación del acceso de un miembro activo: transición de su membresía a `revocado`, de modo que deja de resolver contexto y de aparecer en el selector de Workspace (MVP-104).
- Reenvío de la invitación a una persona en estado `invitado`, por email o por enlace, con el mismo comportamiento que la emisión original de MVP-103 (token nuevo, caducidad renovada, un solo uso).
- Reingreso de un miembro revocado por la vía normal de una nueva invitación (MVP-103).

## Fuera de alcance (out-of-scope)

- Nómina, contratos o datos laborales avanzados.
- Automatización de costes a partir de tarifa horaria.
- Permisos diferenciados entre miembro y trabajador externo.
- Reactivación directa de un miembro revocado sin pasar por una invitación nueva.
- Transferencia de la propiedad (`workspace_owner`) del Workspace entre miembros.
- Gestión de los enlaces compartibles anónimos como si fueran personas del Workspace: el canal `enlace` no tiene destinatario, así que no genera una fila `invitado` en la lista de personas.

## Criterios de aceptación

- [ ] **CA-1**: Los miembros del Workspace aparecen automáticamente como responsables seleccionables en el maestro de trabajadores.
- [ ] **CA-2**: El usuario puede crear y mantener trabajadores sin cuenta vinculada dentro del mismo Workspace.
- [ ] **CA-3**: Los trabajadores con histórico pueden inactivarse sin invalidar los registros ya existentes.
- [ ] **CA-4**: El usuario puede ver la lista de personas del Workspace con su estado de membresía, distinguiendo `invitado` (invitación por email pendiente), `activo` y `revocado`.
- [ ] **CA-5**: Una invitación por email pendiente aparece en la lista como persona en estado `invitado`, y al aceptarse esa misma persona pasa a `activo` sin duplicarse.
- [ ] **CA-6**: El usuario puede reenviar la invitación a una persona en estado `invitado`, por email o por enlace, obteniendo el mismo resultado que la emisión original (nuevo enlace de un solo uso y caducidad renovada).
- [ ] **CA-7**: Al revocar el acceso de un miembro, su membresía pasa a `revocado` y deja de resolver contexto activo y de aparecer en su selector de Workspaces, sin borrar el vínculo ni invalidar los registros operativos que ese usuario ya hubiera creado.
- [ ] **CA-8**: El sistema impide dejar el Workspace sin ningún miembro activo: no se puede revocar al último miembro activo ni al `workspace_owner` mientras siga siendo el único propietario.

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
- La administración de miembros llega a esta historia desde dos puntos del registro de `MVP-999` detectados durante `MVP-104`: `P-002` (listar y revocar miembros → HU-3, HU-4, CA-7, CA-8) y `P-003` (materializar el estado `invitado` y reenviar invitaciones → HU-3, HU-5, CA-4, CA-5, CA-6). MVP-104 dejó ya modelados en el dominio los tres estados del catálogo `worker_member_status` y el método `WorkspaceMember.Revoke()`, pero sin endpoint ni pantalla que los usaran; esta historia expone ese comportamiento en API y UI.
- Coherente con permisos planos (RN-034): cualquier miembro activo puede revocar a otro o reenviar una invitación. La única restricción es no dejar el Workspace sin miembro activo ni sin propietario (CA-8). La transferencia de propiedad queda fuera de alcance.
- Reingreso de un miembro revocado: se hace por la vía normal de una nueva invitación (MVP-103). No se ofrece reactivación directa para no abrir una segunda vía de alta paralela a las invitaciones.
- **Decisión de diseño pendiente para el `tech-design` (representación del estado `invitado`):** `workspace_members.user_id` es NOT NULL con FK a `users`, y una persona invitada por email puede no tener cuenta todavía; además el canal `enlace` no tiene destinatario. Por eso el estado `invitado` **no puede materializarse siempre como una fila física en `workspace_members`**. La opción recomendada es una **vista unificada de personas del Workspace** que combine las membresías reales (`workspace_members`: `activo`/`revocado`) con las invitaciones por email pendientes (`workspace_invitations`), proyectadas como `invitado`, y que el reenvío (HU-5) reutilice el emisor de MVP-103. La alternativa (hacer `user_id` nullable y añadir `email` a `workspace_members` para materializar la fila) duplicaría datos que ya viven en `workspace_invitations` y reabriría el modelo de MVP-103; se documenta como descartada salvo que el `tech-design` justifique lo contrario.
- Impacto en MVP-104: nulo. El selector de Workspace activo consulta solo membresías `activo`, así que las personas `invitado` no resuelven contexto ni aparecen en el selector; no requiere retrabajo de lo ya entregado.
