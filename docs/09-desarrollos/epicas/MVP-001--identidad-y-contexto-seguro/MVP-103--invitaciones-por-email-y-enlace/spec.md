---
id: "MVP-103"
tipo: feature
titulo: "Invitaciones por email y enlace"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito A — Base segura y multiusuario"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
depende_de: ["MVP-101", "MVP-102"]
bloquea: ["MVP-104"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["workspaces", "invitaciones", "multiusuario"]
  modulo_path: "03-modulos/"
  componentes: ["invitaciones", "workspace-members", "email-service"]
  etiquetas: ["mvp", "invite", "workspace"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-103 — Invitaciones por email y enlace

## Contexto

El MVP debe soportar colaboración real desde el día uno. La KB cierra dos canales de invitación obligatorios: email y enlace compartible. Sin este flujo, el producto seguiría siendo monousuario en la práctica.

## Objetivo

Permitir que un miembro de un Workspace invite a otro usuario por email o por enlace y que ese destinatario pueda unirse al Workspace objetivo.

## Requisitos de usuario

### HU-1 — Invitar a otra persona al Workspace

**Como** miembro de un Workspace,
**quiero** invitar a otra persona por email o enlace,
**para** compartir la explotación sin procesos manuales externos.

### HU-2 — Unirse al Workspace desde una invitación

**Como** usuario invitado,
**quiero** aceptar una invitación válida,
**para** entrar directamente en el Workspace que me comparten.

## Alcance (in-scope)

- Generación de invitación por email.
- Generación de invitación por enlace compartible.
- Aceptación de invitación válida por usuario autenticado.
- Gestión de estado de membresía básica asociada a la invitación.
- Mensajes mínimos de error para invitaciones inválidas o expiradas, si aplica.

## Fuera de alcance (out-of-scope)

- Roles distintos según tipo de invitación.
- Flujos de aprobación manual por el owner.
- Personalización avanzada del email de invitación.
- Gestión compleja de expiraciones y reenvíos más allá de lo mínimo necesario.

## Criterios de aceptación

- [ ] **CA-1**: Un miembro del Workspace puede emitir una invitación válida por email o por enlace.
- [ ] **CA-2**: Un usuario autenticado puede aceptar la invitación y quedar asociado al Workspace correcto.
- [ ] **CA-3**: El sistema refleja el estado básico de la membresía derivada de la invitación sin introducir roles granulares.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/Sidebar.tsx](../../../../../prototype/terrenario-mvp/src/components/Sidebar.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| AjustesView | RN-035 | falta | No existe flujo de invitacion por email/enlace en el prototipo |
| Sidebar | RN-034 | parcial | Solo referencia de contexto visual de usuario/workspace |

## Notas y decisiones

- Si el envío de email activa cumplimiento condicionado de LSSI/ePrivacy, deberá documentarse al pasar a `aprobado`.
- El enlace compartible no debe abrir una vía de acceso fuera del flujo autenticado del MVP.
