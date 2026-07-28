---
id: "MVP-107"
tipo: feature
titulo: "Invitaciones no bloqueantes y centro de notificaciones"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito A — Base segura y multiusuario"
esfuerzo_estimado: "4d"
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
depende_de: ["MVP-103", "MVP-104", "MVP-106"]
bloquea: []
relacionado_con: ["MVP-199"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["workspaces", "invitaciones", "notificaciones"]
  modulo_path: "03-modulos/"
  componentes: ["invitaciones", "notificaciones", "workspace-members", "ui-shell"]
  etiquetas: ["mvp", "invite", "notifications", "ux"]
  nivel_riesgo: medio
creado_en: "2026-07-25"
actualizado_en: "2026-07-26"
---

# MVP-107 — Invitaciones no bloqueantes y centro de notificaciones

## Contexto

La revisión de cierre de la épica (MVP-199) detectó que el flujo de invitaciones entregado en
MVP-103 obliga a decidir en el momento y puede dejar al usuario sin salida:

- La pantalla de aceptación de invitación (`/invitations/:token`) es una **página-gate**, no un
  modal. Ofrece "Usar otra cuenta" (que cierra sesión, sin sentido en ese contexto) y "Unirme al
  Workspace", que para una invitación de canal email dirigida a otra cuenta falla con **403
  `AUTH_INVITATION_EMAIL_MISMATCH`** solo **después** de pulsar (el preview no valida el email).
- **No se puede rechazar** una invitación: no existe endpoint ni acción de UI. Un invitado con
  email no coincidente solo puede volver al login.
- **No hay ningún aviso in-app** de invitaciones recibidas: el invitado depende del correo o del
  enlace. No existe endpoint que liste las invitaciones **recibidas** por la cuenta autenticada
  (solo el listado de las **emitidas** por el Workspace activo).

Como el producto es multiusuario y Workspace-first desde el día uno, este flujo debe permitir
entrar a la plataforma sin fricción y gestionar las invitaciones cuando el usuario quiera.

## Objetivo

Que al acceder a la plataforma el usuario llegue directamente a su contexto de trabajo y que las
invitaciones se gestionen de forma **no bloqueante**: aceptar o rechazar cuando quiera, con
información clara de antemano, desde un modal descartable y un centro de notificaciones.

## Requisitos de usuario

### HU-1 — Entrar a la plataforma sin que una invitación me bloquee

**Como** usuario con una invitación pendiente,
**quiero** acceder directamente a mi Workspace activo (o al asistente de creación si no tengo
ninguno) al iniciar sesión,
**para** poder usar la plataforma aunque no decida la invitación en ese momento.

### HU-2 — Decidir la invitación con información y sin callejones sin salida

**Como** persona invitada,
**quiero** saber, antes de aceptar, si mi cuenta actual puede aceptar la invitación, y poder
**aceptarla o rechazarla**,
**para** no toparme con un error tras pulsar ni quedarme sin ninguna salida.

### HU-3 — Enterarme de mis invitaciones dentro de la aplicación

**Como** usuario,
**quiero** ver un aviso discreto (campanita) con mis invitaciones pendientes y una bandeja para
gestionarlas,
**para** no depender solo del correo para descubrir que me han invitado.

## Alcance (in-scope)

- **Acceso post-login directo** (punto 7): tras el login se navega al Workspace activo o, si no
  hay ninguno, al asistente de creación (onboarding). La aceptación de invitación deja de ser una
  puerta obligatoria del flujo de entrada.
- **Modal de invitación no bloqueante** (punto 7): al entrar con una invitación nueva pendiente,
  se ofrece en un modal con "Aceptar" y "Rechazar" que **puede cerrarse** dejando la invitación
  **pendiente** para más tarde.
- **Preview con validación de aptitud** (puntos 5 y R-C): el preview de la invitación informa
  **antes de aceptar** si la cuenta autenticada puede aceptarla (p. ej. invitación de email
  dirigida a otra cuenta), sustituyendo el error tardío. Se retira la acción "Usar otra cuenta"
  del contexto de aceptación y siempre existe una salida hacia "mis Workspaces" u onboarding.
- **Rechazo de invitación** (punto 6): acción de usuario para declinar una invitación, con su
  **endpoint backend** correspondiente. Rechazar no cierra sesión y permite continuar operando.
- **Centro de notificaciones** (punto 7 y R-D): campanita en la cabecera con contador de
  pendientes y una **bandeja de invitaciones recibidas** por la cuenta autenticada, con
  aceptar/rechazar desde ahí. Requiere un **endpoint backend que liste invitaciones recibidas**
  por el usuario (hoy inexistente).

## Fuera de alcance (out-of-scope)

- Notificaciones de tipos distintos a invitaciones (p. ej. asignación de tareas a trabajadores,
  RU-31): pertenecen a otra épica y no se incorporan aquí.
- Canales de notificación externos (push, email adicional) más allá del email de invitación ya
  existente en MVP-103.
- Reenvío de invitaciones y administración de miembros (listar/revocar): son alcance de MVP-204
  (puntos P-002/P-003 en MVP-999).
- Personalización o preferencias del centro de notificaciones.

## Criterios de aceptación

- [x] **CA-1**: Al iniciar sesión, el usuario llega directamente a su Workspace activo o al
  asistente de creación si no tiene ninguno; una invitación pendiente no impide el acceso.
  _`OAuthCallback` → `/app`; `RequireWorkspace`/`OnboardingRoute` con salida al asistente; sin
  página-gate obligatoria._
- [x] **CA-2**: El usuario puede **aceptar** o **rechazar** una invitación; antes de aceptar se le
  informa si su cuenta actual puede hacerlo, y en ningún caso queda sin una salida a la
  plataforma. El rechazo no cierra la sesión. _Preview con `viewer.can_accept` (sin PII);
  `POST /api/v1/invitations/{token}/reject` y `.../received/{id}/reject` sin crear membresía._
- [x] **CA-3**: Existe una campanita/centro de notificaciones en la cabecera que muestra el número
  de invitaciones pendientes recibidas y permite gestionarlas (aceptar/rechazar) desde una
  bandeja; el modal de invitación se puede cerrar dejando la invitación pendiente.
  _`GET /api/v1/invitations/received`; `NotificationBell`/`NotificationsContext`; `InvitationModal`
  descartable ("Decidir más tarde")._

## Diseño técnico

- Pendiente de `tech-design.md`. Notas para el refinamiento:
  - Nuevo endpoint backend para **listar invitaciones recibidas** por la cuenta autenticada
    (por email canónico), separado del actual `GET /api/v1/workspaces/invitations` (emitidas).
  - Nuevo endpoint backend de **rechazo** (`POST /api/v1/invitations/{token}/reject` o
    equivalente) que transite el estado de la invitación sin crear membresía.
  - El preview (`GET /api/v1/invitations/{token}`) debe exponer si la cuenta actual es apta sin
    filtrar PII del destinatario (hoy `PreviewInvitationHandler` no compara email).
  - El modelo de "notificación" del MVP se limita a invitaciones; no se introduce una tabla
    genérica de notificaciones salvo que el tech-design lo justifique.

## Maquetas y referencias visuales

- Referencia UI (cabecera/shell): [prototype/terrenario-mvp/src/components/TopNavbar.tsx](../../../../../prototype/terrenario-mvp/src/components/TopNavbar.tsx)
- Superficie actual a rediseñar: `src/frontend/terrenario-web/src/components/invitations/AcceptInvitationPage.tsx`

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Notas y decisiones

- Historia derivada de la revisión de épica **MVP-199** (consolidación de puntos 5, 6 y 7, más
  los hallazgos R-C y R-D).
- Decisión con el PO (2026-07-25): el punto 7 entra **completo** en la épica (modal no bloqueante,
  campanita y bandeja de invitaciones recibidas), en lugar de diferir el centro de notificaciones.
- Amplía el flujo entregado en MVP-103 sin cambiar sus reglas de negocio (un solo uso, caducidad
  7 días, email vs enlace); añade rechazo y visibilidad in-app.
