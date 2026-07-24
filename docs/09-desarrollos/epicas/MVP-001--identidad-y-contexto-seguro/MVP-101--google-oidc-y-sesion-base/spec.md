---
id: "MVP-101"
tipo: feature
titulo: "Acceso con Google OIDC y sesión base"
estado: completado
prioridad: critica
sprint: ""
hito: "Hito A — Base segura y multiusuario"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
depende_de: []
bloquea: ["MVP-102", "MVP-104", "MVP-105"]
relacionado_con: ["MVP-006"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["autenticacion", "seguridad"]
  modulo_path: "03-modulos/"
  componentes: ["google-oidc", "sesion", "auth-api"]
  etiquetas: ["mvp", "auth", "oidc"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-101 — Acceso con Google OIDC y sesión base

## Contexto

El MVP solo admite Google como proveedor real de identidad y no permite contraseña local. Sin un flujo de autenticación mínimo, el producto no puede crear contexto de usuario ni proteger el resto de operaciones por Workspace.

## Objetivo

Permitir que un usuario se autentique con Google y obtenga una sesión válida del sistema en un flujo corto y estable.

## Requisitos de usuario

### HU-1 — Iniciar sesión con Google

**Como** usuario del sistema,
**quiero** acceder con mi cuenta de Google,
**para** empezar a usar Terrenario sin crear contraseña local.

### HU-2 — Reanudar una sesión válida

**Como** usuario recurrente,
**quiero** mantener una sesión válida mientras uso la aplicación,
**para** no repetir el login innecesariamente dentro del mismo flujo de trabajo.

## Alcance (in-scope)

- Integración base con Google OIDC.
- Alta implícita o vinculación de usuario tras primer login válido.
- Emisión o persistencia de sesión de aplicación.
- Manejo básico de login fallido o cancelado.
- Mensajes de error seguros sin exponer PII ni credenciales.

## Fuera de alcance (out-of-scope)

- Otros proveedores de identidad.
- Passkeys.
- Recuperación de cuenta con contraseña local.
- Observabilidad avanzada del login más allá de la señal mínima definida en MVP.

## Criterios de aceptación

- [ ] **CA-1**: Un usuario puede iniciar sesión con Google y acceder a la aplicación sin crear contraseña local.
- [ ] **CA-2**: Si el login falla o se cancela, el sistema informa el estado sin exponer información sensible.
- [ ] **CA-3**: La sesión autenticada queda disponible para las operaciones posteriores del MVP.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/LandingPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LandingPage.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| LoginPage | RN-018, RN-036, RN-020 | parcial | Prueba manual: login UI y navegacion a diario; pendiente OIDC real |
| LandingPage | RN-018 | cubierto | Prueba visual: CTA a login/onboarding |

## Notas y decisiones

- Debe respetar RN-017, RN-018 y RN-036.
- Esta historia es base técnica para el resto de historias de la épica.
