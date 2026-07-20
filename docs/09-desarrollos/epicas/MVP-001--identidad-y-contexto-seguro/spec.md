---
id: "MVP-001"
tipo: epica
titulo: "Identidad y contexto seguro de Workspace"
estado: borrador
prioridad: critica
hito: "Hito A — Base segura y multiusuario"
tickets: []
historias: ["MVP-101", "MVP-102", "MVP-103", "MVP-104", "MVP-105"]
depende_de: []
bloquea: ["MVP-002", "MVP-003", "MVP-004", "MVP-005", "MVP-006"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["autenticacion", "workspaces", "seguridad"]
  modulo_path: "03-modulos/"
  componentes: ["google-oidc", "workspace-members", "invitaciones"]
  etiquetas: ["mvp", "auth", "workspace"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# EPICA MVP-001 — Identidad y contexto seguro de Workspace

## Contexto

El MVP nace como producto multiusuario y Workspace-first. Toda la operativa, los permisos y la visibilidad de datos dependen del Workspace activo. Sin este bloque, el resto de funcionalidades quedaría sin perímetro de seguridad ni contexto de autorización consistente.

La KB ya fija Google OIDC como único proveedor real del MVP, permisos planos por Workspace y flujo de invitaciones por email y enlace. Esta épica materializa esas decisiones y habilita el resto del roadmap.

## Objetivo

Permitir que un usuario pueda autenticarse con Google, crear o unirse a un Workspace y operar siempre dentro de un contexto de Workspace activo en un flujo de baja fricción.

## Requisitos de usuario de alto nivel

- **Como** usuario del sistema, **quiero** acceder con Google sin contraseña local, **para** empezar a usar la aplicación con la mínima fricción posible.
- **Como** miembro de un Workspace, **quiero** crear, aceptar y alternar Workspaces, **para** operar siempre sobre el contexto correcto de datos.

## Alcance

- Alta y login con Google OIDC como único proveedor real del MVP.
- Creación de Workspace y flujo de primer acceso.
- Invitaciones a Workspace por email y por enlace compartible.
- Gestión de membresía básica (`invitado`, `activo`, `revocado`).
- Selector visible de Workspace activo.
- Autorización plana por Workspace en backend y frontend.
- Trazabilidad mínima del embudo de login para medir éxito/abandono.

## Fuera de alcance

- Otros proveedores de identidad distintos de Google.
- Roles granulares, permisos por recurso o permisos por terreno.
- Passkeys.
- Baja avanzada de cuenta más allá del mínimo necesario para MVP.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [ ] **CA-2**: Un usuario puede autenticarse con Google, crear o aceptar un Workspace y operar dentro del Workspace activo sin pasos manuales fuera del flujo principal.
- [ ] **CA-3**: Todas las operaciones operativas del MVP se ejecutan con ámbito de Workspace y rechazan acceso fuera de ese contexto.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-101` — Acceso con Google OIDC y sesión base.
- `MVP-102` — Creación de Workspace y primer acceso guiado.
- `MVP-103` — Invitaciones por email y enlace compartible.
- `MVP-104` — Membresía y selector de Workspace activo.
- `MVP-105` — Autorización por Workspace y trazabilidad mínima de login.

## Notas y decisiones

- Esta épica es prerequisito de todo el MVP.
- Se asume modelo de permisos completamente plano dentro del Workspace durante MVP.
- La invitación por email y por enlace se considera parte del alcance base, no una mejora opcional.